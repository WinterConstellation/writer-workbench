# Muvel 관찰 기반 Writer Workbench 반영 계획

작성일: 2026-06-24

## 목표

Muvel의 구조를 Writer Workbench에 그대로 옮기지 않는다. Writer Workbench의 핵심인 Windows 전용 작가 작업대, command 기반 커스터마이징, 대형 원고 안정성을 유지하면서 Muvel에서 확인된 좋은 저장 단위를 흡수한다.

## 우선순위

### P0: 현재 구조 보호

이미 구현된 다음 원칙은 유지한다.

- 바인더/장면 목록은 본문 전체를 다시 읽지 않는다.
- 입력 루프에는 export, snapshot, 전체 index rebuild를 넣지 않는다.
- export는 명시 버튼/명령으로만 실행한다.
- snapshot은 수동/확인 기반으로 동작한다.
- 장면 본문, metadata, TXT 파생본, index는 서로 역할을 나눈다.

### P1: Scene metadata 강화

Muvel의 `.mvle`에서 확인한 회차 metadata를 Writer Workbench의 `SceneMetadata`에 단계적으로 반영한다.

추가 후보:

```txt
ContentLength
ContentLengthWithSpaces
SceneType
ManualLineBreak
CreatedAt
UpdatedAt
Tags
Summary
```

효과:

- 바인더/아웃라인/검토 패널에서 본문 로드 없이 글자 수와 상태를 표시할 수 있다.
- 대형 원고에서 scene list refresh 비용을 낮게 유지한다.
- export와 snapshot의 대상 필터링이 쉬워진다.

주의:

- 기존 `SceneStatus`와 충돌하지 않게 migration/default 값을 둔다.
- 글자 수 계산은 저장 시점 또는 명시 refresh에서만 한다.
- 타이핑할 때마다 전체 장면 글자 수를 재계산하지 않는다.

### P1: 설정 파일 기능별 분리

Muvel은 설정을 기능별 JSON으로 나누고 있다. Writer Workbench도 app-wide 설정을 한 파일에 몰지 않는다.

권장 구조:

```txt
AppData\Local\WriterWorkbench\settings\
  app.json
  editor-profiles.json
  theme-profiles.json
  workspace-presets.json
  window-placement-presets.json
  command-assignments.json
  text-replacements.json
  export-profiles.json
  migration-state.json
```

효과:

- 기능 추가가 쉬워진다.
- 설정 손상 시 복구 범위가 작다.
- 사용자 커스터마이징을 import/export하기 쉽다.

### P1: Editor/Theme profile 분리

사용자가 요청한 그래픽 프리셋은 단순 색상 테마가 아니라 글쓰기 표면 설정이어야 한다.

초기 preset:

```txt
기본
검은색 계열
눈이 편한 색상 1
눈이 편한 색상 2
눈이 편한 색상 3
라벤더 색상
```

profile에 포함할 후보:

```txt
fontFamily
fontSize
lineHeight
paragraphGap
editorMaxWidth
editorPadding
foreground
background
selectionColor
caretColor
panelColor
typewriterMode
showTypographicMarks
hardBreakIndent
```

주의:

- WPF 입력 성능을 깨지 않게 style 적용은 preset 전환 시에만 한다.
- 대형 문서 typing 중에는 테마 재적용을 자동 실행하지 않는다.

### P1: Widget Registry MVP

Muvel의 widget instance 구조는 Writer Workbench의 command 기반 작업대와 잘 맞는다.

Writer Workbench 모델 초안:

```txt
WidgetInstance
  Id
  WidgetId
  Context
  Surface
  Area
  Order
  Frame
  CommandId
  Parameters
```

Surface 후보:

```txt
MainWindow
DetachedWindow
FullscreenWriting
Inspector
Toolbar
ContextMenu
```

Area 후보:

```txt
Left
Right
Bottom
Top
Floating
```

첫 MVP 범위:

- 버튼/패널 슬롯만 저장한다.
- 실제 drag-and-drop 편집은 보류한다.
- command id를 widget/button에 배정하는 구조를 먼저 만든다.

### P1: Text Replacement MVP

Muvel의 text replacement 구조는 작고 바로 쓸 수 있다.

모델 초안:

```txt
TextReplacementRule
  Id
  From
  To
  UseRegex
  Enabled
  ApplyMode
```

ApplyMode 후보:

```txt
Manual
OnSpace
OnSave
```

초기 구현 권장:

- `Manual`만 먼저 구현한다.
- 선택 영역 또는 현재 장면에 명시적으로 적용한다.
- regex rule은 허용하되 기본 off로 둔다.

타이핑 루프 주의:

- `TextChanged`에서 모든 regex rule을 매번 돌리지 않는다.
- 자동 치환은 나중에 작은 입력 단위로만 붙인다.

### P2: Export Profile

현재 TXT Export MVP를 유지하면서 export 설정을 profile로 확장한다.

모델 초안:

```txt
ExportProfile
  Id
  Name
  Format
  ParagraphSpacing
  DialogueNarrationSpacing
  SeparatorReplacement
  IncludeComments
  RemoveLineBreaksBetweenDialogues
  ForceLineBreakPerSentence
```

초기 구현:

- TXT profile만 추가한다.
- 기존 export 명령이 default profile을 사용하게 한다.
- DOCX/PDF/EPUB는 보류한다.

### P2: Window Placement Preset 강화

Muvel의 창 상태 구조는 단순하지만 Writer Workbench 요구는 더 크다. 다중 모니터와 탭 분리창 프리셋을 지원해야 한다.

모델 초안:

```txt
WindowPlacementPreset
  Id
  Name
  AutoApplyOnStartup
  DisplayBindings
  Windows

WindowPlacement
  WindowRole
  SceneId
  X
  Y
  Width
  Height
  Maximized
  Fullscreen
  ScreenSlot
```

원칙:

- 물리 monitor id가 바뀔 수 있으므로 screen slot을 사용자 지정으로 둔다.
- startup auto apply는 preset별 on/off로 둔다.
- 적용 실패 시 기본 모니터에 안전하게 모은다.

### P3: Wiki/Resources

Muvel은 `wiki`와 `resources`를 프로젝트 안에 둔다. Writer Workbench도 구조는 준비하되 기능은 늦춘다.

초기 구조:

```txt
resources\
  images\
wiki\
```

보류 이유:

- 현재 P0/P1은 장면, 작업대, export, snapshot이 더 중요하다.
- wiki editor까지 열면 범위가 커진다.

## 구현 순서 제안

1. `SceneMetadata` 강화
2. 설정 저장소 분리
3. Editor/Theme profile MVP
4. Text Replacement MVP
5. Widget Registry MVP
6. Export Profile MVP
7. Window Placement Preset 강화
8. Wiki/Resources MVP

## 테스트 전략

각 기능은 대형 원고 가드레일을 유지해야 한다.

공통 테스트:

- 설정 파일이 없으면 기본값 생성
- 손상된 설정은 해당 기능만 기본값으로 복구
- 대형 본문 없이 metadata만 읽어 바인더 표시
- typing loop에서 export/snapshot/index rebuild가 호출되지 않음
- 설정 변경 후 재실행 복원

기능별 테스트:

- Scene metadata: title/status/order/length 저장과 재로드
- Editor profile: preset 저장/전환/복원
- Text replacement: manual apply, disabled rule skip, regex off 기본값
- Widget registry: command id assignment 저장/복원
- Export profile: TXT export가 profile 옵션을 사용
- Window placement: single monitor fallback, auto apply on/off

## 위험과 대응

위험:

- 설정 기능이 너무 빨리 커져 UI가 복잡해질 수 있다.

대응:

- 내부 모델을 먼저 만들고 UI는 MVP만 노출한다.

위험:

- 자동 텍스트 치환이 대형 원고 입력 렉을 만들 수 있다.

대응:

- 첫 버전은 수동 적용으로 제한한다.

위험:

- 위젯 registry가 command system보다 먼저 커지면 구조가 꼬인다.

대응:

- 모든 widget/button/menu는 command id를 참조하게 한다.

위험:

- 다중 모니터 preset이 사용자마다 다르게 깨질 수 있다.

대응:

- screen slot 수동 지정과 fallback을 둔다.

## 다음 작업 추천

바로 구현한다면 `SceneMetadata 강화 + 설정 파일 분리`를 먼저 한다. 이 두 가지는 Muvel 구조를 참고하면서도 현재 Writer Workbench의 대형 원고 성능 원칙과 충돌하지 않는다.

그 다음 작은 사용자 가치가 큰 `Text Replacement MVP`를 붙인다. 자동 치환은 나중이고, 첫 단계는 명시 명령으로만 실행한다.
