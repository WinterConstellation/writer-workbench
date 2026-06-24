# Muvel 구조 참고 리버스 엔지니어링 계획서

작성일: 2026-06-24

## 목적

Muvel을 복제하거나 실행 파일을 뜯는 것이 목적이 아니다. Writer Workbench가 부족한 "작가용 작업대"의 기본 구조를 보강하기 위해, Muvel의 로컬 저장 구조와 작업 흐름을 안전하게 관찰한다.

이 계획의 산출물은 구현 코드가 아니라 설계 판단 자료다. 특히 다음 항목을 Writer Workbench 설계에 반영할 수 있는지 확인한다.

- 작품, 회차, 자료, 위키, 설정의 분리 방식
- 에디터 설정과 작업공간 설정의 저장 단위
- 위젯/패널/창 상태를 사용자 커스터마이징으로 관리하는 방식
- 텍스트 치환, export, 프로젝트 인덱스 같은 주변 기능의 배치
- 대형 원고를 직접 다시 읽지 않고 metadata/index 중심으로 다루는 방식

## 안전 원칙

이번 조사는 로컬 파일 시스템에 부담을 주지 않는 범위에서만 진행한다.

금지 항목:

- 실행 파일 디컴파일, 디스어셈블, 패킹 해제
- DRM, 라이선스, 인증, 네트워크 호출 우회
- 네트워크 트래픽 캡처
- 원고 본문 추출 또는 복사
- WebView/브라우저 런타임 폴더 접근
- 캐시, DB, lock 파일 대량 접근
- 무제한 재귀 탐색

항상 제외할 경로명:

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

실행 제한:

- 최대 탐색 깊이: 2
- 파일 내용 읽기 전 후보 목록을 먼저 제한
- JSON은 작은 설정/manifest만 읽기
- `.mvle`는 본문 `blocks`의 실제 텍스트를 읽지 않고 key, type, count, length만 확인
- 한 번에 처리할 파일 수에 상한을 둔다
- 각 단계 후 결과를 문서화하고 다음 단계로 넘어간다
- 이상 징후가 있으면 즉시 중단한다

## 이미 확인된 최소 구조

다음 정보는 이전 안전하지 못한 탐색에서 이미 확보된 것으로, 추가 스캔 없이 계획 수립에만 사용한다.

```txt
C:\Users\Masters\AppData\Local\Muvel
C:\Users\Masters\AppData\Local\com.muvel
C:\Users\Masters\AppData\Roaming\com.muvel
C:\Users\Masters\Documents\MuvelProjects
```

관찰된 앱 데이터 구조:

```txt
AppData\Local\com.muvel
  EBWebView\
  kv\
  novel_index.json
  novel_index.json.bak
  novel_item_index.json

AppData\Roaming\com.muvel
  .window-state.json
```

관찰된 프로젝트 구조:

```txt
MuvelProjects\{project-title}\
  {project-title}.muvl
  episodes\{episode-id}.mvle
  resources\images\
  wiki\
```

관찰된 설정 후보:

```txt
kv\muvelAppSettings.json
kv\settings.ai-episode-review-settings.json
kv\settings.episode-io.json
kv\settings.novel-browser.json
kv\settings.onboarding.json
kv\settings.path-restorer.json
kv\settings.settings-legacy-migration.json
kv\settings.text-replacements.json
kv\settings.widget-registry.json
```

관찰된 schema key:

```txt
.muvl:
  id, title, tags, share, createdAt, updatedAt, episodeCount, order, localPath, thumbnailPath

.mvle:
  id, novelId, title, description, contentLength, contentLengthWithSpaces,
  episodeType, status, order, manualLineBreak, createdAt, updatedAt, blocks, comments

novel_index.json:
  id, title, episodeCount, lastOpened, path

novel_item_index.json:
  novelId, itemType
```

## 조사 단계

### 1. 최상위 저장 구조 정리

목표:

- 설치 폴더, 앱 설정 폴더, 사용자 프로젝트 폴더가 어떻게 분리되는지 정리한다.
- Writer Workbench의 `project`, `app settings`, `workspace preset`, `cache/index` 분리 기준을 만든다.

허용 작업:

- 이미 알려진 최상위 경로 존재 여부 확인
- 경로 이름과 파일 확장자만 기록

금지 작업:

- WebView/캐시/DB 폴더 진입
- 설치 폴더 전체 재귀 탐색

### 2. 앱 설정 구조 분석

목표:

- 설정이 단일 거대 파일인지, 기능별 JSON인지 확인한다.
- editor, workspace, export, widget, text replacement 설정을 Writer Workbench 명령 시스템과 어떻게 연결할지 판단한다.

허용 작업:

- `kv` 아래 작은 JSON 파일의 top-level key 확인
- 사용자 원고와 무관한 설정 값의 타입 확인

금지 작업:

- 인증/토큰/계정 정보 출력
- 대형 값 전체 출력

Writer Workbench 반영 후보:

- `AppSettings`
- `EditorProfile`
- `WorkspaceProfile`
- `WidgetRegistry`
- `TextReplacementRules`
- `ExportProfile`

### 3. 작품/회차 파일 구조 분석

목표:

- `.muvl`과 `.mvle`의 역할 분리를 확인한다.
- Writer Workbench의 scene/document/metadata/manifest 구조와 비교한다.

허용 작업:

- `.muvl` key/type/size 확인
- `.mvle` key/type/size 확인
- `blocks`는 개수와 타입만 확인

금지 작업:

- `blocks` 안의 실제 본문 출력
- comments의 실제 사용자 텍스트 출력
- 전체 프로젝트 일괄 읽기

Writer Workbench 반영 후보:

- 작품 단위 manifest 강화
- 장면 metadata에 `contentLength`, `contentLengthWithSpaces`, `status`, `order` 저장
- 본문 파일과 metadata/index 완전 분리
- 대형 본문 로드 없이 바인더/아웃라인 표시

### 4. 작업공간/위젯 구조 추론

목표:

- Muvel이 창 상태와 위젯 배치를 어떻게 저장하는지 확인한다.
- Writer Workbench의 Command Registry, Workspace Preset, UI Slot 구조에 필요한 저장 모델을 정한다.

허용 작업:

- `.window-state.json`의 top-level key 확인
- `settings.widget-registry.json`의 instance 구조 확인

금지 작업:

- 창 내부 WebView 상태 파일 접근
- 브라우저 세션/쿠키/스토리지 접근

Writer Workbench 반영 후보:

- `WorkspacePreset`
- `WindowPlacementPreset`
- `PanelSlot`
- `ToolbarSlot`
- `CommandAssignment`
- `DetachedWindowLayout`

### 5. Writer Workbench 설계 반영안 작성

목표:

- 바로 구현할 항목과 보류할 항목을 분리한다.
- Muvel 구조 참고 결과를 Writer Workbench의 기존 MVP 흐름과 충돌 없이 연결한다.

즉시 반영 후보:

- 설정 JSON 기능별 분리
- text replacement MVP
- workspace/widget registry MVP
- 장면 metadata에 length/count/order/status 강화
- 최근 프로젝트/최근 장면 복구 구조 강화

보류 후보:

- AI 회차 리뷰
- 고급 export 템플릿
- 이미지 리소스 관리
- wiki 고급 편집
- 복잡한 위젯 marketplace 구조

## Writer Workbench 목표 구조 초안

```txt
{project}.writerproj\
  manifest.json
  documents\
    {scene-id}\
      document.wwdoc.json
      document.txt
      scene.meta.json
  exports\
  snapshots\
  resources\
    images\
  wiki\
  indexes\
    search.sqlite
```

앱 설정 초안:

```txt
AppData\Local\WriterWorkbench\
  settings\
    app.json
    editor-profiles.json
    workspace-presets.json
    command-assignments.json
    text-replacements.json
    export-profiles.json
```

핵심 원칙:

- 본문 저장과 화면 표시용 metadata를 분리한다.
- 바인더, 아웃라인, 검색, export는 가능한 한 manifest/metadata/index를 먼저 사용한다.
- 장면 본문은 사용자가 실제로 열 때만 로드한다.
- 모든 UI 동작은 command id로 등록하고 단축키/버튼/패널/프리셋에 배정 가능하게 한다.
- 대형 원고 입력 루프에는 export, snapshot, 전체 index rebuild를 연결하지 않는다.

## 검증 기준

계획 실행 후에는 다음 질문에 답할 수 있어야 한다.

- Muvel은 작품과 회차를 어떤 파일 단위로 나누는가?
- 앱 설정은 어떤 기능 단위로 분리되는가?
- 작업공간/위젯/창 상태는 어떤 key 구조를 가지는가?
- Writer Workbench의 현재 scene/document/metadata 모델과 충돌하는 지점은 무엇인가?
- 당장 구현할 수 있는 작은 기능은 무엇인가?
- 대형 원고 입력 성능을 깨는 위험 요소는 무엇인가?

## 완료 산출물

조사 실행 후 다음 문서를 추가 또는 갱신한다.

- `docs/research/muvel-structure-report.md`
- `docs/research/muvel-to-writer-workbench-plan.md`
- `docs/pingpong.txt`

이번 문서는 조사 전 안전 계획서이며, 별도 데이터 산출물은 없다.
