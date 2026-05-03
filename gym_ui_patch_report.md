# 헬스장 운영기 UI 구조 수정 보고서

이 패치는 업로드된 경량화 Unity 프로젝트 ZIP 기준으로 작성되었습니다.

## 수정 파일

- Assets/_Project/Scripts/Runtime/UI/GeneratedRuntimeSprites.cs
- Assets/_Project/Scripts/Runtime/UI/SimpleGameUIBootstrap.cs
- Assets/_Project/Scripts/Runtime/UI/RuntimeGameUIController.cs
- Assets/_Project/Scripts/Runtime/UI/RuntimeGameUIController.cs.meta
- Assets/_Project/Scripts/Runtime/UI/GameRuntimeUIController.cs
- Assets/_Project/Scripts/Runtime/UI/TitleMenuUIController.cs
- Assets/_Project/Scripts/Editor/UiPrefabAutoRegenerator.cs
- Assets/_Project/Scenes/Title.unity
- Assets/_Project/Scenes/TestSandbox.unity

## 핵심 변경

1. UiPrefabAutoRegenerator의 스크립트 리로드 자동 재생성 제거
2. Title 씬의 RuntimeTitleUIRoot 편집 모드 Sprite 참조 보수
3. TitleMenuUIController가 기존 씬 UI를 발견하면 RectTransform을 초기화하지 않고 버튼/Sprite만 보수하도록 수정
4. SimpleGameUIBootstrap을 MonoBehaviour로 변경하고 RuntimeGameUIController를 별도 파일로 분리
5. TestSandbox의 RuntimeGameUIRoot 기존 구조를 유지하면서 운영/설치 PreviewMode 전환 추가
6. 중앙 헬스장 공간의 가짜 벽/기구/장식 루트 비활성화
7. GeneratedRuntimeSprites가 Resources.Load<Sprite>를 우선 사용하도록 변경

## 검증 메모

- Unity Editor 직접 실행은 이 환경에서 불가능하여 Unity Console 검증은 사용자가 Unity 6000.3.10f1에서 확인해야 합니다.
- 이 환경에는 dotnet/mcs/csc가 없어 C# 컴파일은 실행하지 못했습니다.
- 대신 주요 수정 C# 파일의 중괄호/괄호 균형, 중복 클래스 검색, 씬 YAML 구조와 Sprite GUID 직렬화 여부를 정적 확인했습니다.
