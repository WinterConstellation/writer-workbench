# LLL 구조 관찰 보고서

작성일: 2026-06-24

## 범위

이 보고서는 Writer Workbench 설계 참고용으로 LLL의 로컬 저장 구조를 관찰한 결과다. 실행 파일 분석, 네트워크 분석, 인증 우회, WebView 내부 탐색, 캐시/DB/락 파일 접근은 하지 않았다.

사용한 관찰 방식:

- 알려진 최상위 폴더만 확인
- `kv` 아래 작은 JSON 설정 파일의 key/type 확인
- 작품 폴더 1개에서 `.lllproj` schema 확인
- 회차 파일 1개에서 `.lllscene` schema 확인
- 회차 본문 값은 출력하지 않음
- 프로젝트명/사용자 본문은 보고서에 기록하지 않음

제외한 폴더명:

- `EBWebView`
- `Cache`
- `Code Cache`
- `GPUCache`
- `Crashpad`
- `Network`
- `IndexedDB`
- `Local Storage`
- `Session Storage`
- `Service Worker`

## 최상위 구조

LLL은 설치 파일, 앱 상태, 사용자 프로젝트를 분리한다.

```txt
AppData\Local\LLL
AppData\Local\com.lll
AppData\Roaming\com.lll
Documents\LLLProjects
```

관찰된 앱 데이터:

```txt
AppData\Local\com.lll
  EBWebView\                 # 제외
  kv\
  novel_index.json
  novel_index.json.bak
  novel_item_index.json

AppData\Roaming\com.lll
  .window-state.json
```

의미:

- WebView 런타임 상태와 앱 설정이 같은 app-data 영역에 있지만, 설정은 `kv`와 작은 JSON에 모여 있다.
- 프로젝트 목록과 프로젝트 item index가 앱 전역 index로 분리되어 있다.
- 창 상태는 roaming 쪽에 별도 저장된다.

## 프로젝트 구조

관찰된 프로젝트 폴더 구조:

```txt
{project}\
  episodes\
  resources\
  wiki\
  {project}.lllproj
```

의미:

- 작품 metadata는 `.lllproj` 하나가 담당한다.
- 회차 본문/metadata는 `episodes\*.lllscene`로 분리된다.
- 자료와 위키는 본문 저장 단위와 분리되어 있다.
- Writer Workbench도 `documents`, `resources`, `wiki`, `exports`, `snapshots`, `indexes`를 분리하는 방향이 맞다.

## 작품 파일 `.lllproj`

관찰된 key:

```txt
id
title
tags
share
createdAt
updatedAt
episodeCount
order
localPath
thumbnailPath
```

역할:

- 작품 자체의 id/title/tags/order를 가진다.
- 회차 수와 마지막 갱신 시간을 metadata로 가진다.
- 실제 회차 본문을 직접 품지 않는다.

Writer Workbench 대응:

- `manifest.json` 또는 `.writerproj` manifest에 작품 metadata를 집중시킨다.
- 장면 목록, 순서, 상태, 글자 수 요약은 본문 파일이 아니라 manifest/metadata에서 먼저 읽는다.

## 회차 파일 `.lllscene`

관찰된 key:

```txt
id
novelId
title
description
contentLength
contentLengthWithSpaces
episodeType
status
order
manualLineBreak
createdAt
updatedAt
blocks
comments
```

역할:

- 회차는 단일 파일 안에 metadata와 block 배열을 함께 가진다.
- `contentLength`, `contentLengthWithSpaces`, `status`, `order`가 회차 metadata에 들어 있다.
- `blocks`와 `comments`는 배열 구조다.

Writer Workbench 대응:

- 현재처럼 `document.wwdoc.json`, `document.txt`, `scene.meta.json`을 분리하는 방향은 유지한다.
- 대신 `scene.meta.json`에는 LLL식 요약 metadata를 더 적극적으로 넣는다.
- 바인더/아웃라인/상태 표시가 본문 로드 없이 가능해야 한다.

권장 metadata:

```txt
sceneId
title
status
order
contentLength
contentLengthWithSpaces
sceneType
manualLineBreak
createdAt
updatedAt
tags
summary
```

## 앱 설정 구조

`lllAppSettings.json`의 큰 축:

```txt
episodeEditorStyle
episodeEditorOptions
wikiEditorStyle
wikiEditorOptions
workspaceStyle
workspaceOptions
exportSettings
viewOptions
widgetSettings
editorStyle
```

의미:

- 회차 에디터와 위키 에디터가 별도 style/options를 가진다.
- export, view, workspace, widget 설정이 같은 앱 설정 안에서 분리되어 있다.
- legacy `editorStyle`도 존재하므로 설정 migration을 겪은 흔적이 있다.

Writer Workbench 대응:

- 설정을 하나의 거대 JSON으로 두지 말고 기능별 파일로 분리한다.
- migration 기록을 별도 보관해 장기 호환성을 확보한다.

권장 설정 파일:

```txt
settings\app.json
settings\editor-profiles.json
settings\workspace-presets.json
settings\command-assignments.json
settings\text-replacements.json
settings\export-profiles.json
settings\migration-state.json
```

## 에디터 스타일/옵션

관찰된 style key 예시:

```txt
lineHeight
fontSize
fontWeight
textAlign
indent
blockGap
fontFamily
color
backgroundColor
selectionColor
caretColor
textareaBgColor
widgetColor
editorMaxWidth
editorPaddingX
rightWidgetMaxWidth
leftWidgetMaxWidth
hardBreakIndent
```

관찰된 option key 예시:

```txt
typewriter
typewriterStrict
autoSpellErrorScroll
autoSymbolReplacement
lineBreakImportStrategy
autoQuotes
smartQuotes
autoBrackets
smartBrackets
showTypographicMarks
episodeDynamicLink
episodeEditorToolbar
defaultManualLineBreak
autoChangeStatusOnContentEdit
quickFixOnClick
spellcheckAiValidation
```

의미:

- LLL은 "글 쓰는 표면" 자체를 매우 세밀하게 설정한다.
- 단순 테마보다 editor profile 개념이 강하다.

Writer Workbench 대응:

- 그래픽 프리셋은 theme만이 아니라 editor profile까지 포함해야 한다.
- 기존 요청의 `기본`, `검은색 계열`, `눈이 편한 색상 1/2/3`, `라벤더 색상`은 editor profile preset으로 저장한다.
- `typewriter`, `hardBreakIndent`, `manualLineBreak`, `typographic marks`는 나중에 명령으로 승격한다.

## Export 설정

관찰된 export key:

```txt
paragraphSpacing
dialogueNarrationSpacing
separatorReplacement
spacingBeforeSeparator
spacingAfterSeparator
forceLineBreakPerSentence
includeComments
removeLineBreaksBetweenDialogues
format
```

`settings.episode-io.json`에는 export format 계층도 있다.

```txt
format
richLayout
html
epub
msWord
textLayout
htmlLineSpacingPolicy
```

의미:

- export는 단순 파일 저장이 아니라 layout profile에 가깝다.
- TXT MVP 이후에는 export profile이 필요하다.

Writer Workbench 대응:

- 현재 TXT Export MVP는 유지한다.
- 다음 단계에서 `ExportProfile`을 추가하고, TXT 줄간격/문단 간격/구분자 치환을 command 옵션으로 뺀다.

## 텍스트 치환

관찰된 key:

```txt
rules
```

rule object key:

```txt
from
to
useRegexp
enabled
```

의미:

- Writer Workbench에 바로 넣기 좋은 작은 기능이다.
- 사용자가 원하는 "커스터마이징 가능한 작업대"와 잘 맞는다.

Writer Workbench 대응:

- `TextReplacementRule`을 추가한다.
- typing loop에 직접 무거운 작업을 넣지 말고, 명시 명령/가벼운 inline transform부터 시작한다.
- regex rule은 비활성/수동 적용부터 시작하는 것이 안전하다.

## 위젯 레지스트리

관찰된 key:

```txt
instances
```

instance object key:

```txt
id
widgetId
context
surface
area
order
frame
```

의미:

- LLL은 위젯을 고정 UI가 아니라 instance로 관리한다.
- `context`, `surface`, `area`, `order`, `frame`은 Writer Workbench의 패널/툴바/분리창 배정 모델과 바로 대응된다.

Writer Workbench 대응:

- `Command Registry` 다음 축으로 `WidgetRegistry`를 둔다.
- UI 슬롯은 `surface`, `area`, `order`, `frame`을 가진다.
- 단축키/버튼/패널/우클릭 메뉴/프리셋이 같은 command id를 참조하게 한다.

## 창 상태

`.window-state.json`의 관찰 key:

```txt
main
  width
  height
  x
  y
  prev_x
  prev_y
  maximized
  visible
  decorated
  fullscreen
```

의미:

- 기본 창 상태는 단순한 rectangle + 상태 flag로 충분하다.
- 다중 모니터 프리셋도 이 구조에서 확장 가능하다.

Writer Workbench 대응:

- `WindowPlacementPreset`은 monitor id를 절대 신뢰하지 말고 사용자가 지정한 screen slot을 기준으로 저장한다.
- 실행 시 자동 적용은 preset id 단위로 on/off한다.
- 모니터 구성이 바뀌면 사용자에게 재배정 UI를 제공한다.

## 결론

LLL에서 Writer Workbench가 참고할 핵심은 화려한 기능 목록이 아니라 저장 단위와 설정 분리다.

가장 중요한 구조적 시사점:

1. 작품 manifest와 회차 파일을 분리한다.
2. 회차 metadata에 글자 수, 상태, 순서, 줄바꿈 정책을 둔다.
3. 앱 설정은 기능별 JSON으로 분리한다.
4. 위젯은 고정 UI가 아니라 registry instance로 다룬다.
5. export는 단순 저장이 아니라 profile로 다룬다.
6. 텍스트 치환은 작고 즉시 가치가 있는 커스터마이징 기능이다.
7. 창 상태와 작업공간 프리셋은 별도 계층으로 분리한다.

Writer Workbench의 기존 방향 중 유지할 것:

- 본문 파일과 metadata/index 분리
- binder refresh가 본문 전체를 읽지 않는 구조
- snapshot/export가 입력 루프에 붙지 않는 구조
- command id 중심의 기능 등록

Writer Workbench의 다음 보강점:

- 기능별 설정 파일 분리
- editor profile/theme profile 분리
- widget registry MVP
- text replacement MVP
- export profile MVP
- scene metadata의 길이/order/status 강화
