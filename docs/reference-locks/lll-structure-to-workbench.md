# LLL Structure To Writer Workbench Reference Lock

작성일: 2026-06-26 KST

## 목적

이 문서는 LLL 관찰 결과를 Writer Workbench 구현 기준으로 변환한다. 목표는 LLL을 복제하는 것이 아니라, Writer Workbench의 Windows 전용 작가 작업대 구조에 필요한 저장 단위, 설정 분리, 작업대 shell, widget/command 배치 방식을 흡수하는 것이다.

## 확인한 로컬 구조

관찰 범위는 알려진 설치 폴더와 작은 설정 JSON의 구조 확인으로 제한했다.

```txt
C:\Users\Masters\AppData\Local\Muvel
  muvel.exe
  uninstall.exe
  icons\

C:\Users\Masters\AppData\Local\com.muvel
  EBWebView\
  kv\
  novel_index.json
  novel_index.json.bak
  novel_item_index.json
```

`EBWebView`가 존재하므로, 앱 내부에 WebView 기반 작업 표면을 두는 접근 자체는 비정상 구조가 아니다. 문제는 WebView 화면을 WPF 도구창 안에 보조 화면처럼 끼워 넣는 것이다. Writer Workbench의 HTML 작업대는 전체 작업 표면을 주도하고, WPF는 파일, 저장, 창 제어, 네이티브 명령 실행을 담당해야 한다.

## 복제 금지 범위

다음은 하지 않는다.

```txt
LLL 실행 파일 분석
LLL 코드 또는 자산 복사
아이콘/이미지/고유 명칭 복제
사용자 원고/개인 데이터 읽기
WebView 캐시, IndexedDB, Login Data 접근
LLL 파일 포맷 호환 구현
UI를 픽셀 단위로 복제
```

가져올 수 있는 것은 추상 구조뿐이다.

```txt
기능별 설정 분리
작품 index와 프로젝트 저장 단위 분리
에디터 style/options 분리
widget registry 기반 화면 배치
text replacement 독립 설정
export profile 독립 설정
마지막 위치/탭 복원
WebView 기반 전체 작업대 shell
```

## Writer Workbench 목표 구조

Writer Workbench는 WPF 앱 안에 HTML을 다시 넣은 보조 화면이 아니라, 다음 구조를 가져야 한다.

```txt
WPF host
  - 프로젝트 파일 접근
  - autosave/save/export/snapshot
  - 창 분리와 화면 배치
  - 단축키 처리
  - WebView2 bridge

HTML workbench shell
  - 상단 메뉴
  - 바인더/설정집/자료/관계도/타임라인 표면
  - 현재 작업 surface
  - inspector/widget slots
  - floating remote control
```

HTML은 장식용 preview가 아니라 사용자가 보는 기본 작업 표면이다. WPF 기본 컨트롤 화면은 fallback 또는 네이티브 host 역할로 축소한다.

## 설정 분리 기준

LLL의 `kv` 구조를 Writer Workbench에 맞게 기능별 설정 파일로 변환한다.

```txt
settings\
  app.json
  editor-profiles.json
  workspace-options.json
  widget-registry.json
  command-assignments.json
  text-replacements.json
  export-profiles.json
  path-restorer.json
  migration-state.json
```

각 파일은 다음 책임만 가진다.

```txt
app.json
  앱 공통 옵션, 마지막 프로젝트, autosave 기본값

editor-profiles.json
  글쓰기 표면의 font, line height, paragraph gap, max width, padding, caret/selection 색상

workspace-options.json
  마지막 탭, 마지막 surface, 좌우 패널 열림 상태, 작업대 표시 모드

widget-registry.json
  어떤 command/widget이 어느 surface와 area에 배치되는지

command-assignments.json
  command id와 단축키, 메뉴, 리모컨 슬롯 배정

text-replacements.json
  수동/저장 시점/입력 시점 치환 규칙

export-profiles.json
  TXT export 옵션, 문단 간격, 대사/서술 분리, 줄바꿈 규칙

path-restorer.json
  마지막 프로젝트, 마지막 장면, 마지막 화면 위치

migration-state.json
  설정 schema version, migration 완료 기록
```

## Workbench Shell 기준

HTML 작업대는 다음 화면 구조를 기준으로 재작성한다.

```txt
Top command menu
  작품 / 원고 / 구조 / 보기 / 도구 / 설정

Left rail
  바인더
  설정집
  자료
  검색 결과

Center surface
  원고 편집
  장면 정보
  관계도
  타임라인
  내보내기

Right inspector
  현재 장면 정보
  태그/요약
  snapshot
  관련 인물/장소

Bottom status
  자동저장 상태
  글자 수
  현재 명령
  대형 작업 진행률

Floating remote
  명령 버튼
  아이콘만 보기 / 아이콘+제목 보기
  크기 조절
  항상 위
  화면 밖 일부 이동 허용
```

## Command And Widget 기준

모든 버튼은 고정 UI가 아니라 command registry에서 온다.

```txt
Command
  Id
  Label
  Category
  DefaultShortcut
  CanPlaceInMenu
  CanPlaceInToolbar
  CanPlaceInRemote
  CanPlaceInContextMenu

WidgetInstance
  Id
  WidgetId
  Surface
  Area
  SlotKey
  Order
  CommandId
  Parameters
```

초기 구현에서는 drag-and-drop widget 편집까지 하지 않는다. 먼저 저장 가능한 슬롯 구조와 렌더링을 연결한다.

## Data Flow

```txt
ProjectStore/SceneStore/MetadataStore
  -> WebWorkbenchPayloadFactory
  -> WebView2 postMessage(state)
  -> HTML render

HTML command click
  -> WebView2 postMessage(commandId)
  -> MainWindow command dispatcher
  -> WPF/Core service
  -> state refresh
```

입력 루프에는 export, snapshot, full index rebuild, full manuscript scan을 넣지 않는다. 대형 원고 기준으로 본문 전체 계산은 저장 시점 또는 명시 refresh에서만 수행한다.

## 첫 구현 범위

다음 패치의 범위는 이 문서 기준으로 제한한다.

```txt
1. AppSettings 계층 추가
2. settings/*.json 기능별 store 추가
3. widget-registry store 추가
4. HTML workbench shell을 전체 작업대 중심으로 재작성
5. command registry에서 상단 메뉴와 리모컨 슬롯 렌더링
6. path-restorer로 마지막 surface/scene 복원 연결
```

하지 않을 것:

```txt
LLL 포맷 호환
AI/API 연동
관계도 고급 canvas 엔진
타임라인 전체 구현
WPF MainWindow 전체 재작성
대형 원고 성능 계측 깊게 파기
```

## 수용 기준

구현 완료 판정은 다음 기준으로 한다.

```txt
앱 실행 시 HTML workbench가 기본 작업 표면으로 보인다.
WPF 도구창 화면이 중복으로 보이지 않는다.
상단 메뉴와 리모컨 버튼은 command registry 기반으로 렌더링된다.
settings 파일이 기능별로 생성/로드된다.
마지막 surface/active scene을 재실행 후 복원한다.
대형 원고 입력 루프에 새 전체 스캔 작업이 추가되지 않는다.
dotnet build WriterWorkbench.sln --no-restore 통과
dotnet test tests\WriterWorkbench.Tests\WriterWorkbench.Tests.csproj --no-restore 통과
```

## 레퍼런스 사용 규칙

이 문서 이후 UI/작업대 구현은 다음 절차 없이 시작하지 않는다.

```txt
1. 참고한 레퍼런스 구조를 문서에 적는다.
2. 그대로 복제하지 않을 요소를 명시한다.
3. 이번 패치에서 맞출 항목을 체크리스트로 잠근다.
4. 구현 후 실행 화면 또는 테스트로 차이를 확인한다.
5. docs/pingpong.txt에 결과와 남은 문제를 쓴다.
```

