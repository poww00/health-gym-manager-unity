# 헬스장 운영기 UI 리빌드용 Codex 프롬프트 팩

- 버전: 2026-04-22
- 목적: 기존 Unity UI를 억지 수정하지 않고, 타이틀부터 인게임 HUD, 하단 탭, 운영/설치/경제/리뷰 패널, 직원/채용/게임 메뉴 팝업까지 전부 새 스프라이트 자산 조립 구조로 재설계하기 위한 기준 문서.
- 작업 전제:
  - 현재 프로젝트에는 `Assets/_Project/Sprites/UI`와 `Assets/_Project/Sprites/GeneratedUI`가 혼재해 있으므로, 리빌드 1차 작업은 새 경로에 병행 구축한다.
  - 권장 스테이징 경로:
    - `Assets/_Project/Sprites/UI_Rebuild/`
    - `Assets/_Project/Prefabs/UIRebuild/`
  - 텍스트는 기본적으로 Unity TMP로 처리한다.
  - 이미지에 텍스트를 넣는 예외는 타이틀 로고 정도만 허용한다.

## 0. Codex에 먼저 붙여넣을 시작 프롬프트

```text
너는 지금 Unity 기반 모바일 2D 픽셀 경영 타이쿤 프로젝트의 UI 리빌드를 맡는다.

프로젝트명:
- 헬스장 운영기
- 앱인토스용 모바일 게임
- 카이로소프트풍 2D 픽셀 경영 타이쿤

중요 배경:
- 기존 UI 구현은 레이아웃, 스크롤, 폰트, 패딩, 안전영역, 베이스 프레임 침범 문제가 누적되어 부분 수정으로 살릴 수 없다.
- 따라서 타이틀부터 상단 HUD, 하단 탭 바, 운영/설치/경제/리뷰 패널, 직원/채용/게임 메뉴 팝업까지 전부 새로 설계한다.
- 목표는 코드 생성형 임시 UI가 아니라, 분리된 PNG 스프라이트 자산을 Unity에서 9-slice, Sprite Swap, Prefab 조립으로 사용하는 구조다.
- 텍스트는 가능하면 이미지에 박지 말고 Unity TextMeshPro로 넣는다.

프로젝트 핵심 컨셉:
- 현실 동네 헬스장 운영 기반의 카이로소프트풍 2D 픽셀 경영 타이쿤
- 핵심 성장 축: 부지 고정 운영 -> 월말 결산 -> 이사로 확장
- 부지 확장 구조: 8x8 -> 16x16 -> 32x32 -> 64x64
- 기구 브랜드 등급: B / A / S / SS
- 좋은 브랜드일수록 회원층(매너, 객단가, 상류층 비중)이 바뀐다.
- 플랫폼은 앱인토스용 모바일이다.

절대 원칙:
1. 기존 깨진 UI를 이어붙이지 말 것.
2. 타이틀부터 전체 UI 구조를 새로 설계할 것.
3. 스프라이트는 반드시 분리 자산으로 만들 것.
4. 모든 패널은 프레임 내부 안전영역이 명확해야 한다.
5. 운영 패널 정도의 가독성과 크기를 전체 UI의 기준 스케일로 삼을 것.
6. 설치 패널은 2열 카드형 리스트 + 패널 전체 스크롤 구조를 전제로 설계할 것.
7. 경제/리뷰 패널은 운영 패널과 같은 크기감으로 읽혀야 한다.
8. 팝업은 공통 헤더, 닫기 버튼, 탭, 리스트 행 프리팹 구조로 재사용 가능해야 한다.
9. image-2 자산은 반드시 투명 배경의 개별 PNG로 뽑을 것.
10. 결과물은 Unity에서 9-slice, Sprite Swap, Prefab 조립이 가능한 형태를 우선할 것.

이번 작업에서 네가 해야 할 일:
- 전체 UI를 자산 단위로 분해한다.
- 어떤 자산을 image-2로 먼저 생성해야 하는지 우선순위를 정한다.
- 자산별 목적, 상태(normal/active/disabled/selected), 네이밍 규칙, Unity 사용 방식을 정리한다.
- 각 화면용 와이어 구조도를 텍스트로 정리한다.
- 기존 깨진 배치를 정답처럼 취급하지 말고, 목업과 가독성 중심으로 다시 판단한다.

추가 요청:
기존 깨진 UI 코드를 억지로 이어붙이는 방향으로 가지 말고, 새 스프라이트 자산을 조립하는 구조를 전제로 판단해라.
특히 설치 패널은 2열 카드 스크롤, 경제/리뷰 패널은 운영 패널과 동등한 가독성, 모든 패널은 베이스 프레임 내부 안전영역 준수를 최우선으로 삼아라.
```

## 1. 리빌드 기준

### 1.1 화면 스케일 기준

- 기준 해상도: `1080 x 1920` 세로 화면
- 기준 스케일 앵커:
  - 상단 HUD: 화면 높이의 약 `10% ~ 12%`
  - 하단 탭 바: 화면 높이의 약 `11% ~ 13%`
  - 메인 패널 본체: 상단 HUD와 하단 탭 바 사이의 주 콘텐츠 영역
  - 패널 내부 여백: 기준 해상도에서 최소 `32 ~ 48px`
- 운영 패널을 전체 UI의 기준 밀도로 삼고, 경제/리뷰는 동일한 읽힘을 유지한다.
- 설치 패널은 정보량이 많아도 카드가 작아 보이면 안 된다. 2열 기준으로 카드 간격과 좌우 여백이 균등해야 한다.

### 1.2 구조 원칙

- 타이틀, 인게임 HUD, 하단 탭, 패널, 팝업을 모두 독립 프리팹으로 나눈다.
- 프레임, 박스, 버튼, 탭, 카드, 스크롤 요소는 공통 자산을 먼저 만든다.
- 화면별로 전용 베이스를 만들더라도 모서리 톤, 두께, 그림자 규칙은 공통으로 맞춘다.
- 한 장짜리 통합 UI 스크린샷 느낌의 이미지는 금지한다.
- 패널 프레임 내부에서 텍스트, 아이콘, 값, 버튼이 침범하지 않도록 inner safe area를 우선한다.

## 2. 전체 UI 자산 목록

### 2.1 공통 프레임/컨트롤 세트

| 자산명 | 용도 | 상태 | 우선순위 | 9-slice | 비고 |
| --- | --- | --- | --- | --- | --- |
| `UI_Common_MainPanel_Base_L` | 운영/설치/경제/리뷰의 공통 대형 패널 베이스 | static | P1 | Yes | 상단에 서브탭이 걸칠 수 있는 구조 |
| `UI_Common_SectionBox_M` | 중형 정보 섹션 박스 | static | P1 | Yes | 2분할/리스트/요약용 공통 박스 |
| `UI_Common_SummaryBox_S` | 소형 요약 박스 | static | P1 | Yes | 4칸 요약 영역 기준 |
| `UI_Common_Divider_H` | 가로 구분선 | static | P1 | No | 패널 내부 그룹 분리 |
| `UI_Common_Divider_V` | 세로 구분선 | static | P1 | No | 2분할 박스 내부 사용 |
| `UI_Common_ScrollRail_V` | 세로 스크롤 레일 | static | P1 | Yes | 긴 패널에 대응 |
| `UI_Common_ScrollHandle_V` | 세로 스크롤 핸들 | normal | P1 | No | 마우스/터치 드래그 대응 |
| `UI_Common_Button_Wide_Normal` | 공통 와이드 버튼 기본 | normal | P1 | Yes | 팝업/타이틀 공용 가능 |
| `UI_Common_Button_Wide_Active` | 공통 와이드 버튼 강조 | active | P1 | Yes | 주요 액션용 |
| `UI_Common_Button_Wide_Disabled` | 공통 와이드 버튼 비활성 | disabled | P1 | Yes | 선택 불가 상태 |
| `UI_Common_Tab_M_Normal` | 공통 중형 탭 | normal | P1 | Yes | 서브탭 공용 |
| `UI_Common_Tab_M_Active` | 공통 중형 탭 | active | P1 | Yes | 활성 탭 |
| `UI_Common_Tab_M_Disabled` | 공통 중형 탭 | disabled | P2 | Yes | 잠금/미개방 상태 |
| `UI_Common_Tab_M_Secondary` | 공통 보조 탭 | secondary | P1 | Yes | sky-blue 계열 보조 탭 |

### 2.2 타이틀 세트

| 자산명 | 용도 | 상태 | 우선순위 | 9-slice | 비고 |
| --- | --- | --- | --- | --- | --- |
| `UI_Title_Logo` | 게임 로고 | static | P2 | No | 텍스트 포함 허용 |
| `UI_Title_Banner_Base` | 로고/타이틀 장식 베이스 | static | P2 | No | 간판/리본 느낌 |
| `UI_Title_MenuPanel_Base` | 타이틀 중앙 패널 베이스 | static | P2 | Yes | 저장 슬롯/버튼 수용 |
| `UI_Title_MenuHeader` | 타이틀 전용 상단 장식 헤더 | static | P2 | No | 패널 상단 포인트 |
| `UI_Title_Button_Normal` | 이어하기/새 게임 버튼 | normal | P2 | Yes | 큰 버튼 |
| `UI_Title_Button_Active` | 강조 버튼 | active | P2 | Yes | 새 게임/확인 |
| `UI_Title_SaveSlot_Row` | 저장 슬롯 행 | normal | P2 | Yes | 가로 슬롯 |
| `UI_Title_SaveSlot_Row_Selected` | 저장 슬롯 선택 상태 | selected | P2 | Yes | 강조 테두리 |
| `UI_Title_SaveSlot_ArrowButton` | 슬롯 우측 이동 버튼 | normal | P2 | No | 별도 아이콘 삽입 가능 |
| `UI_Title_SaveSlot_Arrow` | 우측 화살표 아이콘 | static | P2 | No | 고정 이미지 |
| `UI_Title_SaveStatus_Filled` | 저장 데이터 있음 | filled | P3 | No | 상태 아이콘 |
| `UI_Title_SaveStatus_Empty` | 빈 슬롯 | empty | P3 | No | 상태 아이콘 |
| `UI_Title_Backdrop_Frame` | 배경 장식 프레임 | static | P3 | No | 창문/실내 분위기용 |

### 2.3 상단 HUD 세트

| 자산명 | 용도 | 상태 | 우선순위 | 9-slice | 비고 |
| --- | --- | --- | --- | --- | --- |
| `UI_HUD_TopBar_Base` | 상단 HUD 전체 베이스 | static | P1 | Yes | 좌측 정보 + 우측 버튼 영역 |
| `UI_HUD_InfoBox_Small` | 날짜/시간/자금/스타코인 박스 | static | P1 | Yes | 좁은 높이에서도 읽혀야 함 |
| `UI_HUD_Button_Square_Normal` | 직원/메뉴/속도 버튼 | normal | P1 | No | 정사각형 |
| `UI_HUD_Button_Square_Active` | 활성 버튼 | active | P1 | No | 설치 모드/속도 강조 |
| `UI_HUD_Button_Square_Disabled` | 비활성 버튼 | disabled | P2 | No | 잠금 상태 대응 |
| `UI_HUD_IconSlot_Frame` | 아이콘 홀더 | static | P2 | No | 작은 아이콘 프레임 |
| `UI_HUD_ModeBadge` | 설치 모드 상태 표시 배지 | active | P2 | Yes | 망치 아이콘과 조합 |

### 2.4 하단 탭 바 세트

| 자산명 | 용도 | 상태 | 우선순위 | 9-slice | 비고 |
| --- | --- | --- | --- | --- | --- |
| `UI_BottomNav_Base` | 하단 네비게이션 바 전체 베이스 | static | P1 | Yes | 4개 탭 수용 |
| `UI_BottomNav_Tab_Normal` | 운영/설치/경제/리뷰 탭 | normal | P1 | Yes | 라벨은 TMP |
| `UI_BottomNav_Tab_Active` | 활성 탭 | active | P1 | Yes | green 계열 |
| `UI_BottomNav_Tab_Disabled` | 잠금 탭 | disabled | P2 | Yes | 추후 확장 대응 |
| `UI_BottomNav_IconFrame` | 탭 아이콘 프레임 | static | P2 | No | 아이콘과 조합 |

### 2.5 운영 패널 세트

| 자산명 | 용도 | 상태 | 우선순위 | 9-slice | 비고 |
| --- | --- | --- | --- | --- | --- |
| `UI_Operate_SubTab_Normal` | 운영 패널 상단 서브탭 | normal | P1 | Yes | 공통 탭 재사용 가능 |
| `UI_Operate_SubTab_Active` | 운영 패널 활성 서브탭 | active | P1 | Yes | green 또는 sky-blue |
| `UI_Operate_SummaryRow_4Slot` | 상단 요약 4칸 베이스 | static | P1 | Yes | SummaryBox 4개 조합형 |
| `UI_Operate_InfoBox_Dual` | 중간 2칸 정보 박스 | static | P1 | Yes | 2분할 정보 영역 |
| `UI_Operate_MemoBox` | 하단 메모/공지/상태 박스 | static | P1 | Yes | 긴 문장 대응 |

### 2.6 설치 패널 세트

| 자산명 | 용도 | 상태 | 우선순위 | 9-slice | 비고 |
| --- | --- | --- | --- | --- | --- |
| `UI_Install_CategoryTab_Normal` | 유산소/근력/회복/편의 탭 | normal | P2 | Yes | 4개 고정 |
| `UI_Install_CategoryTab_Active` | 활성 카테고리 탭 | active | P2 | Yes | green 강조 |
| `UI_Install_CategoryTab_Disabled` | 잠김 탭 | disabled | P3 | Yes | 필요 시 |
| `UI_Install_Card_Base` | 2열 카드형 리스트 카드 | normal | P2 | Yes | 좌측 아이콘 박스, 우측 정보 |
| `UI_Install_Card_Selected` | 선택 카드 | selected | P2 | Yes | 외곽 강조 |
| `UI_Install_Card_Disabled` | 구매 불가 카드 | disabled | P2 | Yes | 어두운 톤 |
| `UI_Install_StatusButton_Normal` | 카드 내부 상태 버튼 | normal | P2 | Yes | 배치/선택 |
| `UI_Install_StatusButton_Active` | 카드 내부 강조 버튼 | active | P2 | Yes | 구매 가능/확정 |
| `UI_Install_StatusButton_Disabled` | 카드 내부 비활성 버튼 | disabled | P2 | Yes | 자금 부족 등 |
| `UI_Install_SelectionBar` | 하단 선택 상태 바 | static | P2 | Yes | 선택 기구/가격 요약 |

### 2.7 경제 패널 세트

| 자산명 | 용도 | 상태 | 우선순위 | 9-slice | 비고 |
| --- | --- | --- | --- | --- | --- |
| `UI_Economy_SubTab_Normal` | 경제 패널 서브탭 | normal | P2 | Yes | 운영 패널과 공통 가능 |
| `UI_Economy_SubTab_Active` | 경제 패널 활성 탭 | active | P2 | Yes | 공통 탭 재사용 가능 |
| `UI_Economy_SummaryBox` | 상단 요약 박스 | static | P2 | Yes | 운영과 같은 크기감 |
| `UI_Economy_DualInfoBox` | 중간 2분할 박스 | static | P2 | Yes | 항목/값 정렬 |
| `UI_Economy_LedgerBox` | 하단 결산 상세 박스 | static | P2 | Yes | 다중 행 수용 |

### 2.8 리뷰 패널 세트

| 자산명 | 용도 | 상태 | 우선순위 | 9-slice | 비고 |
| --- | --- | --- | --- | --- | --- |
| `UI_Review_SubTab_Normal` | 리뷰 패널 서브탭 | normal | P2 | Yes | 공통 탭 재사용 가능 |
| `UI_Review_SubTab_Active` | 리뷰 패널 활성 탭 | active | P2 | Yes | 공통 탭 재사용 가능 |
| `UI_Review_SummaryBox` | 상단 요약 박스 | static | P2 | Yes | 읽힘 우선 |
| `UI_Review_ListBox` | 최근 리뷰 박스 | static | P2 | Yes | 리뷰 행 반복 |
| `UI_Review_EventLogBox` | 이벤트 로그 박스 | static | P2 | Yes | 로그 행 반복 |
| `UI_Review_EmptyStateBox` | 빈 상태 메시지 박스 | static | P2 | Yes | 단문 메시지 수용 |

### 2.9 팝업 세트

| 자산명 | 용도 | 상태 | 우선순위 | 9-slice | 비고 |
| --- | --- | --- | --- | --- | --- |
| `UI_Popup_Base_Large` | 직원/채용/게임 메뉴용 대형 팝업 | static | P2 | Yes | 제목줄/리스트 영역 포함 |
| `UI_Popup_Base_Medium` | 확인/경고용 중형 팝업 | static | P3 | Yes | 공통 확인창 |
| `UI_Popup_HeaderStrip` | 팝업 상단 헤더 스트립 | static | P2 | Yes | 제목 배경 |
| `UI_Popup_Close_Normal` | 닫기 버튼 | normal | P2 | No | 고정형 버튼 |
| `UI_Popup_Close_Active` | 닫기 버튼 눌림/활성 | active | P2 | No | Sprite Swap |
| `UI_Popup_Action_Wide_Green` | 주요 액션 버튼 | active | P2 | Yes | 채용/확정 |
| `UI_Popup_Action_Wide_Yellow` | 강조/이사 버튼 | emphasis | P2 | Yes | 주의/강조 |
| `UI_Popup_Action_Wide_Beige` | 중립 버튼 | normal | P2 | Yes | 닫기/취소 |
| `UI_Popup_PageArrow_Left` | 좌측 페이지 버튼 | normal | P3 | No | 필요 시 |
| `UI_Popup_PageArrow_Right` | 우측 페이지 버튼 | normal | P3 | No | 필요 시 |
| `UI_Popup_ListHeader_Row` | 직원/지원자 리스트 헤더 | static | P2 | Yes | 열 제목용 |
| `UI_Popup_ListRow_Base` | 직원/지원자 행 프리팹 베이스 | normal | P2 | Yes | 스크롤 행 반복 |
| `UI_Popup_ListRow_Selected` | 선택 행 | selected | P3 | Yes | 선택 강조 |
| `UI_Popup_EmptyStateBox` | 빈 상태 박스 | static | P2 | Yes | 지원자 없음 등 |

### 2.10 아이콘 세트

| 자산명 | 용도 | 상태 | 우선순위 | 9-slice | 비고 |
| --- | --- | --- | --- | --- | --- |
| `UI_Icon_Clock` | 시간 | static | P3 | No | HUD |
| `UI_Icon_Cash` | 자금 | static | P3 | No | HUD |
| `UI_Icon_StarCoin` | 스타코인 | static | P3 | No | HUD |
| `UI_Icon_User` | 직원 | static | P3 | No | HUD/팝업 |
| `UI_Icon_Menu` | 메뉴 | static | P3 | No | HUD |
| `UI_Icon_Play` | 재생 | static | P3 | No | 속도 버튼 |
| `UI_Icon_FastForward` | 배속 | static | P3 | No | 속도 버튼 |
| `UI_Icon_Hammer` | 설치 모드 | static | P3 | No | HUD/설치 |
| `UI_Icon_Clipboard` | 운영 | static | P3 | No | 하단 탭 |
| `UI_Icon_Graph` | 경제 | static | P3 | No | 하단 탭 |
| `UI_Icon_Star` | 리뷰 | static | P3 | No | 하단 탭 |
| `UI_Icon_Cardio` | 유산소 | static | P3 | No | 설치 카테고리 |
| `UI_Icon_Weight` | 근력 | static | P3 | No | 설치 카테고리 |
| `UI_Icon_Recovery` | 회복 | static | P3 | No | 설치 카테고리 |
| `UI_Icon_Convenience` | 편의 | static | P3 | No | 설치 카테고리 |
| `UI_Icon_Close` | 닫기 | static | P3 | No | 팝업 |
| `UI_Icon_LeftArrow` | 좌측 이동 | static | P3 | No | 팝업/타이틀 |
| `UI_Icon_RightArrow` | 우측 이동 | static | P3 | No | 팝업/타이틀 |
| `UI_Icon_Check` | 확인 | static | P3 | No | 상태 강조 |
| `UI_Icon_Lock` | 잠금 | static | P3 | No | 비활성 상태 |
| `UI_Icon_Alert` | 경고 | static | P3 | No | 자금 부족/이벤트 |
| `UI_Icon_Treadmill` | 런닝머신 | static | P3 | No | 설치 카드 |
| `UI_Icon_PremiumTreadmill` | 고급 런닝머신 | static | P3 | No | 설치 카드 |
| `UI_Icon_BenchPress` | 벤치프레스 | static | P3 | No | 설치 카드 |
| `UI_Icon_PunchingBag` | 샌드백 | static | P3 | No | 설치 카드 |
| `UI_Icon_StretchMat` | 스트레칭 매트 | static | P3 | No | 설치 카드 |
| `UI_Icon_WaterDispenser` | 정수기 | static | P3 | No | 설치 카드 |

## 3. 자산별 우선순위

### P1. 프레임 시스템과 화면 골격

- `UI_Common_*` 전반
- `UI_HUD_*`
- `UI_BottomNav_*`
- `UI_Operate_*`

이 단계만 완료되어도 운영 패널 기준의 실제 조립 테스트가 가능하다. UI 전체 스케일, 안전영역, 탭 전환 구조를 여기서 먼저 고정한다.

### P2. 화면별 핵심 생산 자산

- `UI_Title_*`
- `UI_Install_*`
- `UI_Economy_*`
- `UI_Review_*`
- `UI_Popup_*`

이 단계에서 실제 플레이 중 가장 많이 보는 화면과 팝업 흐름이 완성된다.

### P3. 의미 전달 보강 자산

- `UI_Icon_*`
- 저장 상태 아이콘, 장식 프레임, 페이지 화살표
- 비활성/잠김/빈 상태 변형

이 단계는 완성도를 올리는 구간이다. 기본 플레이 흐름은 P1~P2만으로 먼저 검증한다.

## 4. 상태별 분리 목록

| 자산군 | 필수 상태 |
| --- | --- |
| 공통 와이드 버튼 | `normal`, `active`, `disabled` |
| 공통 탭 / 서브탭 | `normal`, `active`, `disabled` |
| HUD 정사각 버튼 | `normal`, `active`, `disabled` |
| 하단 탭 버튼 | `normal`, `active`, `disabled` |
| 설치 카테고리 탭 | `normal`, `active`, `disabled` |
| 설치 카드 | `normal`, `selected`, `disabled` |
| 설치 카드 내부 상태 버튼 | `normal`, `active`, `disabled` |
| 타이틀 저장 슬롯 | `normal`, `selected`, `empty` |
| 팝업 닫기 버튼 | `normal`, `active` |
| 팝업 행 프리팹 | `normal`, `selected` |

상태별 이미지는 Sprite Swap 중심으로 쓰고, 라벨과 숫자는 TMP로만 교체한다.

## 5. Unity 폴더 구조

### 5.1 권장 스프라이트 구조

```text
Assets/_Project/Sprites/UI_Rebuild/
Assets/_Project/Sprites/UI_Rebuild/Common/
Assets/_Project/Sprites/UI_Rebuild/Title/
Assets/_Project/Sprites/UI_Rebuild/HUD/
Assets/_Project/Sprites/UI_Rebuild/BottomNav/
Assets/_Project/Sprites/UI_Rebuild/Panels/Operate/
Assets/_Project/Sprites/UI_Rebuild/Panels/Install/
Assets/_Project/Sprites/UI_Rebuild/Panels/Economy/
Assets/_Project/Sprites/UI_Rebuild/Panels/Review/
Assets/_Project/Sprites/UI_Rebuild/Popups/
Assets/_Project/Sprites/UI_Rebuild/Icons/Common/
Assets/_Project/Sprites/UI_Rebuild/Icons/Equipment/
Assets/_Project/Sprites/UI_Rebuild/Decor/
```

### 5.2 권장 프리팹 구조

```text
Assets/_Project/Prefabs/UIRebuild/
Assets/_Project/Prefabs/UIRebuild/Common/
Assets/_Project/Prefabs/UIRebuild/Title/
Assets/_Project/Prefabs/UIRebuild/HUD/
Assets/_Project/Prefabs/UIRebuild/Panels/
Assets/_Project/Prefabs/UIRebuild/Popups/
Assets/_Project/Prefabs/UIRebuild/Rows/
Assets/_Project/Prefabs/UIRebuild/Cards/
```

### 5.3 네이밍 규칙

- 프리팹: `PF_UI_*`
- 스프라이트: `UI_*`
- TMP 스타일/프리셋: `TMP_UI_*`
- 예시:
  - `PF_UI_TopHUD`
  - `PF_UI_BottomNav`
  - `PF_UI_OperatePanel`
  - `PF_UI_InstallCard`
  - `PF_UI_Popup_GameMenu`
  - `TMP_UI_Label_S`
  - `TMP_UI_Value_M`

## 6. 9-slice / 고정 이미지 / TMP 분리 규칙

### 6.1 9-slice 대상

- 메인 패널 베이스
- 섹션 박스
- 요약 박스
- 설치 카드 베이스
- 설치 카드 상태 버튼
- 팝업 베이스
- 팝업 헤더 스트립
- 와이드 버튼류
- 하단 탭 버튼
- 카테고리 탭 버튼
- 스크롤 레일
- 선택 상태 바

### 6.2 고정 이미지 대상

- 로고
- 아이콘 전반
- 닫기 버튼
- 화살표 버튼
- 작은 장식 파츠
- 저장 상태 아이콘

### 6.3 TMP로 처리할 요소

- 버튼 라벨
- 탭 라벨
- 날짜, 시간, 자금, 스타코인 수치
- 기구명
- 가격
- 통계 수치
- 설명 문구
- 리뷰 텍스트
- 이벤트 로그
- 직원/지원자 이름과 능력치

## 7. 화면 와이어 구조도

### 7.1 타이틀 화면

```text
SafeArea
└─ TitleBackdropFrame
   ├─ TitleLogo
   ├─ TitleBannerBase
   ├─ TitleMenuPanel
   │  ├─ PrimaryButtonRow
   │  │  ├─ ContinueButton
   │  │  └─ NewGameButton
   │  └─ SaveSlotList
   │     ├─ SaveSlotRow
   │     ├─ SaveSlotRow
   │     └─ SaveSlotRow
   └─ AmbientDecor
```

### 7.2 인게임 기본 셸

```text
SafeArea
├─ TopHUD
├─ PanelHost
│  ├─ OperatePanel
│  ├─ InstallPanel
│  ├─ EconomyPanel
│  └─ ReviewPanel
├─ BottomNav
└─ PopupLayer
```

### 7.3 운영 패널

```text
OperatePanel
├─ SubTabRow (2)
├─ SummaryGrid (4)
├─ DualInfoSection
└─ MemoNoticeStateBox
```

### 7.4 설치 패널

```text
InstallPanel
├─ CategoryTabRow (4)
├─ CategoryDescriptionBox
├─ ScrollViewport
│  └─ CardGrid (2 columns)
│     ├─ EquipmentCard
│     ├─ EquipmentCard
│     └─ ...
└─ SelectionBar
```

### 7.5 경제 패널

```text
EconomyPanel
├─ SubTabRow (2)
├─ SummaryGrid (4)
├─ DualInfoSection
└─ LedgerDetailBox
```

### 7.6 리뷰 패널

```text
ReviewPanel
├─ SubTabRow (2)
├─ SummaryGrid (4)
├─ RecentReviewBox
├─ EventLogBox
└─ EmptyStateBox
```

### 7.7 팝업 공통 구조

```text
PopupShell
├─ HeaderStrip
│  ├─ TitleLabel
│  └─ CloseButton
├─ TabRow (optional)
├─ ListHeader (optional)
├─ ScrollViewport / ContentArea
└─ ActionButtonRow
```

## 8. Unity에서 조립할 때 필요한 prefab 구조 제안

### 8.1 최상위 프리팹

- `PF_UIRoot_Canvas`
  - `SafeAreaRoot`
  - `PF_UI_TopHUD`
  - `PF_UI_BottomNav`
  - `PanelHost`
  - `PopupLayer`

### 8.2 패널 프리팹

- `PF_UI_OperatePanel`
  - `PF_UI_SubTabRow_2`
  - `PF_UI_SummaryGrid_4`
  - `PF_UI_DualInfoBox`
  - `PF_UI_MemoBox`
- `PF_UI_InstallPanel`
  - `PF_UI_CategoryTabRow_4`
  - `PF_UI_CategoryDescriptionBox`
  - `PF_UI_ScrollView_InstallGrid`
  - `PF_UI_InstallSelectionBar`
- `PF_UI_EconomyPanel`
  - `PF_UI_SubTabRow_2`
  - `PF_UI_SummaryGrid_4`
  - `PF_UI_DualInfoBox`
  - `PF_UI_LedgerBox`
- `PF_UI_ReviewPanel`
  - `PF_UI_SubTabRow_2`
  - `PF_UI_SummaryGrid_4`
  - `PF_UI_ReviewListBox`
  - `PF_UI_EventLogBox`
  - `PF_UI_EmptyStateBox`

### 8.3 팝업 프리팹

- `PF_UI_Popup_GameMenu`
  - 지점 정보 영역
  - 이사 버튼
  - 타이틀 버튼
  - 닫기 버튼
- `PF_UI_Popup_Staff`
  - 탭 2개
  - 리스트 헤더
  - 리스트 스크롤
  - 빈 상태 박스
- `PF_UI_Popup_Recruit`
  - 탭 2개
  - 지원자 리스트
  - `PF_UI_Row_RecruitCandidate`
  - 채용 버튼

### 8.4 재사용 단위 프리팹

- `PF_UI_TabButton`
- `PF_UI_ActionButton_Wide`
- `PF_UI_CloseButton`
- `PF_UI_SummaryBox`
- `PF_UI_SectionBox`
- `PF_UI_ListHeader`
- `PF_UI_ListRow`
- `PF_UI_InstallCard`
- `PF_UI_Scrollbar_V`

### 8.5 컨트롤러 분리 제안

- 상단 HUD는 값 갱신과 버튼 이벤트만 담당한다.
- 하단 탭 바는 현재 패널 전환만 담당한다.
- 운영/설치/경제/리뷰 패널은 각자 별도 컨트롤러로 데이터 바인딩을 가진다.
- 팝업 스택은 `PopupLayer`에서 열고 닫는다.
- `MainHUDController` 하나에 하단 패널 내용, 설치 카드, 팝업, 선택 HUD를 모두 몰아 넣지 않는다.

## 9. image-2 공통 생성 규칙

모든 프롬프트에 아래 규칙을 공통으로 적용한다.

```text
Use case: ui-mockup
Asset type: Unity PNG sprite for a mobile 2D pixel management tycoon game
Style/medium: pixel-art mobile management game UI, Kairosoft-inspired cozy management tone
Color palette: warm beige and gold frame, soft cream interior, thick dark navy outline, green active tab, sky-blue secondary tab
Composition/framing: single isolated front-facing orthographic UI asset
Constraints: transparent background, no full screen composition, no mockup scene, no merged UI sheet, clear inner safe padding, readable and game-ready, export-ready PNG, no text unless explicitly requested
Avoid: modern flat UI, glossy neon look, blurry anti-aliased vector feel
```

추가 규칙:

- 패널은 안쪽 안전영역이 분명해야 한다.
- 프레임 안쪽 컨텐츠가 테두리를 침범하지 않는 구조로 보여야 한다.
- 버튼과 탭은 상태별로 분리 생성한다.
- 긴 패널은 9-slice를 염두에 두고 모서리와 가장자리 명암이 명확해야 한다.
- 아이콘은 검은 실루엣이 아니라 읽기 쉬운 픽셀 아이콘으로 만든다.

## 10. image-2 프롬프트 최종 정리

### 10.1 메인 패널 공통 베이스 세트

#### `UI_Common_MainPanel_Base_L`

```text
Create a single isolated pixel-art main content panel base for a cozy mobile gym management game, transparent background, no text, no mockup, no full screen, warm beige and gold outer frame, dark navy outline, soft cream interior, clear inner safe padding, top area designed to allow hanging sub-tabs, thick readable border, clean game-ready sprite, orthographic front view, export-ready PNG.
```

#### `UI_Common_SectionBox_M`

```text
Create a single isolated pixel-art medium section box for a cozy mobile gym management game, transparent background, no text, warm beige and gold frame, dark navy outline, soft cream interior, clear inner safe padding, readable medium-sized box for labels and values, clean game-ready sprite, orthographic front view, export-ready PNG.
```

#### `UI_Common_SummaryBox_S`

```text
Create a single isolated pixel-art small summary box for a cozy mobile gym management game, transparent background, no text, warm beige frame, dark navy outline, cream interior, readable at small size, clear inner padding, clean game-ready sprite, orthographic front view, export-ready PNG.
```

#### `UI_Common_Divider_H`

```text
Create a single isolated pixel-art horizontal divider line for a cozy mobile management game UI, transparent background, no text, dark navy outline with soft beige highlight, clean readable pixel sprite, export-ready PNG.
```

#### `UI_Common_Divider_V`

```text
Create a single isolated pixel-art vertical divider line for a cozy mobile management game UI, transparent background, no text, dark navy outline with soft beige highlight, clean readable pixel sprite, export-ready PNG.
```

#### `UI_Common_ScrollRail_V`

```text
Create a single isolated pixel-art vertical scroll rail for a cozy mobile management game UI, transparent background, no text, warm beige frame, dark navy outline, readable pixel-art shading, designed for a long panel, 9-slice friendly, export-ready PNG.
```

#### `UI_Common_ScrollHandle_V`

```text
Create a single isolated pixel-art vertical scroll handle for a cozy mobile management game UI, transparent background, no text, warm beige body with dark navy outline and clear highlight, readable at small size, export-ready PNG.
```

### 10.2 타이틀 세트

#### `UI_Title_Logo`

```text
Create a single isolated pixel-art game title logo for a cozy gym management tycoon game called "헬스장 운영기", transparent background, Korean title, charming Kairosoft-inspired style, thick dark outline, warm gold and blue accent colors, readable at mobile size, no background scene, export-ready PNG.
```

#### `UI_Title_Banner_Base`

```text
Create a single isolated pixel-art decorative title banner base for a mobile management game, transparent background, no text, warm beige and gold frame with subtle decorative trim, cozy and slightly premium feeling, Kairosoft-inspired, export-ready PNG.
```

#### `UI_Title_Button_Normal`

```text
Create a single isolated large pixel-art menu button base for a cozy mobile management game, transparent background, no text, warm beige version, thick navy outline, clean readable pixel sprite, 9-slice friendly, export-ready PNG.
```

#### `UI_Title_Button_Active`

```text
Create a single isolated large pixel-art menu button base for a cozy mobile management game, transparent background, no text, green active version, thick navy outline, clean readable pixel sprite, 9-slice friendly, export-ready PNG.
```

#### `UI_Title_SaveSlot_Row`

```text
Create a single isolated pixel-art save slot row for a mobile management game, transparent background, no text, wide horizontal slot with warm beige frame, dark outline, inner cream fill, designed for save slot information, clear inner padding, export-ready PNG.
```

#### `UI_Title_SaveSlot_Row_Selected`

```text
Create a single isolated pixel-art save slot row for a mobile management game, transparent background, no text, selected state, wide horizontal slot with warm beige frame, stronger green highlight accent, dark outline, inner cream fill, clear inner padding, export-ready PNG.
```

#### `UI_Title_SaveSlot_ArrowButton`

```text
Create a single isolated small pixel-art arrow button for a title save slot row, transparent background, no text, warm beige frame, dark navy outline, clean readable pixel sprite, export-ready PNG.
```

### 10.3 상단 HUD 세트

#### `UI_HUD_TopBar_Base`

```text
Create a single isolated pixel-art top HUD bar base for a cozy mobile management game, transparent background, no text, warm beige and gold frame, dark navy outline, left info area and right button area composition, clean safe padding, 9-slice friendly, export-ready PNG.
```

#### `UI_HUD_InfoBox_Small`

```text
Create a single isolated pixel-art small info box for a top HUD, transparent background, no text, warm beige frame, cream fill, dark outline, readable at small size, clear inner padding, export-ready PNG.
```

#### `UI_HUD_Button_Square_Normal`

```text
Create a single isolated pixel-art small square button for a top HUD, transparent background, no text, beige normal state, dark outline, clean readable pixel sprite, export-ready PNG.
```

#### `UI_HUD_Button_Square_Active`

```text
Create a single isolated pixel-art small square button for a top HUD, transparent background, no text, green active state, dark outline, clean readable pixel sprite, export-ready PNG.
```

#### `UI_HUD_IconSlot_Frame`

```text
Create a single isolated pixel-art icon slot frame for a top HUD, transparent background, no text, square icon holder, warm beige frame, dark outline, export-ready PNG.
```

#### `UI_HUD_ModeBadge`

```text
Create a single isolated pixel-art mode badge for a top HUD in a cozy mobile management game, transparent background, no text, green highlighted badge for install mode status, warm beige secondary trim, dark navy outline, export-ready PNG.
```

### 10.4 하단 탭 바 세트

#### `UI_BottomNav_Base`

```text
Create a single isolated pixel-art bottom navigation bar base for a cozy mobile gym management game, transparent background, no text, warm beige and gold frame, dark navy outline, designed to hold four main tabs, 9-slice friendly, export-ready PNG.
```

#### `UI_BottomNav_Tab_Normal`

```text
Create a single isolated pixel-art bottom navigation tab button for a cozy mobile management game, transparent background, no text, beige normal state, dark outline, 9-slice friendly, export-ready PNG.
```

#### `UI_BottomNav_Tab_Active`

```text
Create a single isolated pixel-art bottom navigation tab button for a cozy mobile management game, transparent background, no text, green active state, dark outline, 9-slice friendly, export-ready PNG.
```

### 10.5 운영 패널 세트

#### `UI_Operate_SubTab_Normal`

```text
Create a single isolated pixel-art medium sub-tab button for an operate panel in a cozy mobile management game, transparent background, no text, beige normal state, dark navy outline, readable pixel sprite, 9-slice friendly, export-ready PNG.
```

#### `UI_Operate_SubTab_Active`

```text
Create a single isolated pixel-art medium sub-tab button for an operate panel in a cozy mobile management game, transparent background, no text, green active state with optional sky-blue secondary accent, dark navy outline, readable pixel sprite, 9-slice friendly, export-ready PNG.
```

#### `UI_Operate_SummaryRow_4Slot`

```text
Create a single isolated pixel-art summary row module for a cozy mobile gym management game, transparent background, no text, designed to visually contain four balanced summary slots, warm beige frame, dark outline, clear inner spacing, export-ready PNG.
```

#### `UI_Operate_InfoBox_Dual`

```text
Create a single isolated pixel-art dual information box for a cozy mobile management game, transparent background, no text, two balanced content areas for labels and values, warm beige frame, dark outline, clear inner safe padding, export-ready PNG.
```

#### `UI_Operate_MemoBox`

```text
Create a single isolated pixel-art memo or notice box for a cozy mobile management game, transparent background, no text, warm beige frame, dark outline, spacious inner content area for multi-line notes, export-ready PNG.
```

### 10.6 설치 패널 세트

#### `UI_Install_CategoryTab_Normal`

```text
Create a single isolated pixel-art category tab button for an install panel in a cozy mobile gym management game, transparent background, no text, beige normal state, designed for top row category tabs, dark outline, 9-slice friendly, export-ready PNG.
```

#### `UI_Install_CategoryTab_Active`

```text
Create a single isolated pixel-art category tab button for an install panel in a cozy mobile gym management game, transparent background, no text, green active state, designed for top row category tabs, dark outline, 9-slice friendly, export-ready PNG.
```

#### `UI_Install_Card_Base`

```text
Create a single isolated pixel-art equipment card base for an install panel, transparent background, no text, designed for a two-column scrollable card grid, warm beige frame, cream fill, icon box on the left, title and price area on the right, clear margins, Kairosoft-inspired, 9-slice friendly, export-ready PNG.
```

#### `UI_Install_Card_Selected`

```text
Create a single isolated pixel-art equipment card base for an install panel, transparent background, no text, selected state, warm beige frame with stronger green highlight accent, icon box on the left, title and price area on the right, clear margins, 9-slice friendly, export-ready PNG.
```

#### `UI_Install_Card_Disabled`

```text
Create a single isolated pixel-art equipment card base for an install panel, transparent background, no text, disabled state, muted warm beige frame with darker cream interior and subdued contrast, icon box on the left, title and price area on the right, export-ready PNG.
```

#### `UI_Install_StatusButton_Active`

```text
Create a single isolated pixel-art small status button for an equipment card, transparent background, no text, green active state, dark outline, clean readable pixel sprite, export-ready PNG.
```

#### `UI_Install_SelectionBar`

```text
Create a single isolated pixel-art install selection bar for the bottom of an install panel, transparent background, no text, designed for selected equipment info and price summary, warm beige frame, dark outline, clear inner safe padding, export-ready PNG.
```

설치 패널 추가 지시:

- 카드가 너무 작아 보이면 안 된다.
- 2열 배치 기준으로 좌우 여백이 균등해야 한다.
- 스크롤 영역 내부 안전영역이 넓어야 한다.
- 카드 안의 글자와 버튼이 들어가도 답답해 보이지 않아야 한다.

### 10.7 경제 패널 세트

#### `UI_Economy_SummaryBox`

```text
Create a single isolated pixel-art summary box for an economy panel in a cozy mobile management game, transparent background, no text, readable medium-sized box, warm beige frame, dark outline, clear inner padding, export-ready PNG.
```

#### `UI_Economy_DualInfoBox`

```text
Create a single isolated pixel-art dual-section information box for an economy panel, transparent background, no text, two balanced areas for labels and values, warm beige frame, dark outline, clear inner safe padding, export-ready PNG.
```

#### `UI_Economy_LedgerBox`

```text
Create a single isolated pixel-art detailed ledger box for an economy panel, transparent background, no text, designed for multiple rows of financial breakdown, warm beige frame, dark outline, spacious inner content area, export-ready PNG.
```

경제 패널 추가 지시:

- 운영 패널과 같은 크기감으로 읽혀야 한다.
- 너무 얇거나 작은 박스는 금지한다.
- 글자가 들어갈 충분한 내부 여백이 있어야 한다.

### 10.8 리뷰 패널 세트

#### `UI_Review_SummaryBox`

```text
Create a single isolated pixel-art summary box for a review panel in a cozy mobile management game, transparent background, no text, readable medium-sized box, warm beige frame, dark outline, clear inner padding, export-ready PNG.
```

#### `UI_Review_ListBox`

```text
Create a single isolated pixel-art review list box for a cozy mobile management game, transparent background, no text, designed for stacked review rows, warm beige frame, dark outline, spacious inner content area, export-ready PNG.
```

#### `UI_Review_EventLogBox`

```text
Create a single isolated pixel-art event log box for a cozy mobile management game, transparent background, no text, designed for stacked event log rows, warm beige frame, dark outline, spacious inner content area, export-ready PNG.
```

#### `UI_Review_EmptyStateBox`

```text
Create a single isolated pixel-art empty state box for a cozy mobile management game, transparent background, no text, simple calm box for empty messages, warm beige frame, dark outline, export-ready PNG.
```

### 10.9 팝업 세트

#### `UI_Popup_Base_Large`

```text
Create a single isolated pixel-art large popup panel for a cozy mobile management game, transparent background, no text, warm beige frame, dark outline, title bar area, close button area, list content area, clear inner padding, 9-slice friendly, export-ready PNG.
```

#### `UI_Popup_HeaderStrip`

```text
Create a single isolated pixel-art popup header strip for a cozy mobile management game, transparent background, no text, warm beige and gold trim, dark outline, 9-slice friendly, export-ready PNG.
```

#### `UI_Popup_Close_Normal`

```text
Create a single isolated pixel-art popup close button for a cozy mobile management game, transparent background, no text, beige normal state, dark outline, export-ready PNG.
```

#### `UI_Popup_Close_Active`

```text
Create a single isolated pixel-art popup close button for a cozy mobile management game, transparent background, no text, pressed or active state, warm beige frame with green highlight, dark outline, export-ready PNG.
```

#### `UI_Popup_Action_Wide_Green`

```text
Create a single isolated pixel-art wide action button for a popup, transparent background, no text, green state, dark outline, 9-slice friendly, export-ready PNG.
```

#### `UI_Popup_Action_Wide_Yellow`

```text
Create a single isolated pixel-art wide action button for a popup, transparent background, no text, yellow emphasis state, dark outline, 9-slice friendly, export-ready PNG.
```

#### `UI_Popup_Action_Wide_Beige`

```text
Create a single isolated pixel-art wide action button for a popup, transparent background, no text, beige neutral state, dark outline, 9-slice friendly, export-ready PNG.
```

#### `UI_Popup_ListHeader_Row`

```text
Create a single isolated pixel-art list header row for a cozy mobile management game popup, transparent background, no text, warm beige frame, dark outline, designed for column labels above a scrollable list, export-ready PNG.
```

#### `UI_Popup_ListRow_Base`

```text
Create a single isolated pixel-art list row base for a cozy mobile management game popup, transparent background, no text, warm beige frame, dark outline, designed for a reusable scroll list item with text and action button space, export-ready PNG.
```

### 10.10 아이콘 세트

공통 프롬프트:

```text
Create a single isolated pixel-art UI icon for a cozy mobile gym management game, transparent background, no text, readable at small size, dark outline, simple and clean pixel silhouette with subtle inner shading, export-ready PNG.
```

생성 대상:

- `UI_Icon_Clock`
- `UI_Icon_Cash`
- `UI_Icon_StarCoin`
- `UI_Icon_User`
- `UI_Icon_Menu`
- `UI_Icon_Play`
- `UI_Icon_FastForward`
- `UI_Icon_Hammer`
- `UI_Icon_Clipboard`
- `UI_Icon_Graph`
- `UI_Icon_Star`
- `UI_Icon_Cardio`
- `UI_Icon_Weight`
- `UI_Icon_Recovery`
- `UI_Icon_Convenience`
- `UI_Icon_Close`
- `UI_Icon_LeftArrow`
- `UI_Icon_RightArrow`
- `UI_Icon_Check`
- `UI_Icon_Lock`
- `UI_Icon_Alert`
- `UI_Icon_Treadmill`
- `UI_Icon_PremiumTreadmill`
- `UI_Icon_BenchPress`
- `UI_Icon_PunchingBag`
- `UI_Icon_StretchMat`
- `UI_Icon_WaterDispenser`

## 11. 실제 생성 순서 추천

1. `UI_Common_*` + `UI_HUD_*` + `UI_BottomNav_*`
2. `UI_Operate_*`
3. `UI_Install_*`
4. `UI_Economy_*` + `UI_Review_*`
5. `UI_Popup_*`
6. `UI_Title_*`
7. `UI_Icon_*`

이 순서의 장점:

- 화면 전체 크기감과 안전영역을 먼저 고정할 수 있다.
- 운영 패널을 기준 척도로 잡은 뒤 설치/경제/리뷰를 같은 눈금으로 맞출 수 있다.
- 팝업과 타이틀은 공통 프레임 시스템이 잡힌 뒤 생성하면 톤이 덜 흔들린다.

## 12. 실행 메모

- 현재 워크스페이스에는 기존 `UI` 스프라이트와 `GeneratedUI` 산출물이 섞여 있으므로, 새 자산은 우선 `UI_Rebuild`에 쌓고 최종 검수 후 교체하는 편이 안전하다.
- 현재 런타임 UI는 즉석 생성 성격이 강하므로, 이번 리빌드에서는 프레임/버튼/카드/리스트/팝업을 각각 독립 프리팹으로 나누는 것이 좋다.
- 최우선 검수 기준은 예쁨보다도 안전영역, 가독성, 터치 타깃 크기, 2열 카드 스크롤의 답답함 여부다.
