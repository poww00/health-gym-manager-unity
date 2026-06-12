# PROJECT_CONTEXT — 헬스장 운영기 2D 픽셀 경영 타이쿤 (개발 컨텍스트)

**현재 공식 방향 (2026-04 기준)**: **순수 탑뷰 (90도 직하 Top-Down)**

- 시점: 정면형 쿼터뷰(Quarter-View) 전환 실험은 **완전 폐기**하고 순수 탑뷰로 공식 복귀.
- 그래픽/바닥: 기존 Warm Floor 타일셋 (`gym_floor_tile_base_warm_final` 등) + GridCell SpriteRenderer 기반 안정화 구현을 공식으로 사용.
- "정면형 쿼터뷰 전환" 관련 모든 실험 기록은 **탑뷰 복귀 완료**로 업데이트됨.
- 이 문서는 개발팀 내부 의사결정과 현재 기술 스택의 진실된 상태를 기록한다.

---

## Section 1. 프로젝트 개요 (변경 없음)
- 앱인토스용 2D 픽셀 헬스장 경영 타이쿤
- 카이로소프트 스타일 월말 결산 + 확장 루프
- 픽셀 아트, 가벼운 번들, 모바일 최적화

## Section 2. 그래픽 / 시점 / 렌더링 전략 (2026-04 업데이트)
**공식 시점**: 순수 탑뷰 (90° Orthographic Top-Down)

- 카메라: Orthographic, Grid에 자동 Fit, 핀치 줌 + 드래그 이동 (심즈 스타일)
- 바닥 렌더링: GridCell 프리팹의 SpriteRenderer + Warm Theme 타일셋 (`GeneratedRuntimeUI/ui_v2/tiles/gym_floor/warm_theme/...`)
  - `gym_floor_tile_base_warm_final`
  - Border Side / Corner warm 타일
- **과거 실험 (참고 기록)**: 
  - 한때 정면형 쿼터뷰(Perspective Quarter-View) 전환을 검토함 (PerspectiveGridFloorVisualizer + mesh quad + gym_floor_tileset_quarterview_v1.png 사용).
  - `floor_wood_plank_a` 등을 이용한 mesh 기반 perspective floor 시도.
  - **결과**: 2026-04, quarter-view 전환 포기. **현재는 순수 탑뷰로 완전 복귀**.
- 모든 quarter-view 관련 mesh/quad/visualizer 코드는 정리 완료.
- 안정화된 Warm Floor Border + 입구/데스크/동선 시각은 그대로 유지.

## Section 3. 바닥 / 타일 시스템 (2026-04 업데이트)
**현재 공식**: GridCell SpriteRenderer + Warm Floor 타일셋

- 기본 바닥: `gym_floor_tile_base_warm_final`
- 테두리: warm_theme의 side / corner 타일 (BuildWarmFloorBorderTiles)
- **과거 실험**: gym_floor_tileset_quarterview_v1.png + PerspectiveGridFloorVisualizer (mesh) 시도 → **폐기**.
- "현재는 순수 탑뷰로 복귀" 상태. Quarter-view 타일셋 로딩 코드 모두 제거.
- 입구/데스크 주변 바닥 특수 처리(안정화 영역)는 절대 건드리지 않음.

## Section 4. Grid / 배치 / 시각 시스템 (2026-04 업데이트)
- GridManager + GridCell 기반 8×8~ 확장 구조 (안정)
- PlacementManager, GymPlacedObjectVisual 등 배치 시스템은 탑뷰 기준으로 동작
- **Quarter-view 관련 코드 정리 완료**:
  - PerspectiveGridFloorVisualizer 비활성화/제거 방향
  - GridManager 내 perspective / quarter-view prototype 필드 및 Apply 메서드 제거
- 안정화된 영역 (입구, 데스크, 고객 동선, Reception) 은 **절대 수정 금지** 규칙 유지.

## Section 5. 절대 준수 규칙 (변경 없음, 중요)
- **되돌리기 제안 절대 금지** — 한 번 안정화된 시스템에 대해 "이전 방식으로 돌아가자"는 제안 금지.
- **기존 안정화 영역 절대 건드리지 않음**: 입구(Entrance), 데스크(Reception), 고객 동선(WalkLane/Waypoint), GridCell 핵심 로직, Save/Load 등.
- 실제 파일·sprite 이름 정확히 확인 후 작업 (예: gym_floor_tile_base_warm_final).
- Warm Floor Border + Entrance/Reception 시각은 현재 공식이며 유지.

## Section 6. 기술 스택 현황
- Unity 6 (6000.3.x)
- URP 2D, SpriteRenderer 중심
- Addressables + Resources 혼용 (타일/프롭은 Resources)
- Input System (터치/마우스)

## Section 7. 다음 작업 우선순위 (2026-04, 순수 탑뷰 기준으로 재작성)
**공식 방향**: 순수 탑뷰 완성 + 콘텐츠 확장

1. **바닥/환경 폴리싱 (최우선)**
   - Warm Floor 타일셋의 타일 경계 seam/dot 문제 (있는 경우) SpriteRenderer + 적절한 UV/9-slice 또는 간단한 inset으로 해결
   - Warm Floor Border의 입구 주변 특수 처리 완성도 높이기 (이미 안정화된 영역은 절대 건드리지 않으면서)

2. **카메라 / UX**
   - 순수 탑뷰 기준 카메라 Fit / 줌 제한 / 부드러운 이동 개선
   - 모바일 터치 조작감 (핀치 + 드래그) 최적화

3. **콘텐츠 확장 (GDD v1.1 기준)**
   - 16×16 / 32×32 부지 확장 시각/로직
   - 기구 4단 브랜드(B/A/S/SS) 시각 차별화
   - 회원/스태프/이벤트 시스템 연동

4. **성능 / 번들**
   - SpriteAtlas 적극 활용
   - Warm Floor / Prop 텍스처 압축 및 mipmap 전략

5. **도구 / 에디터**
   - GymQuarterViewFloorTilesetImporter 등 quarter-view 전용 에디터 도구는 정리 또는 비활성화 대상
   - Warm Floor 타일 검증 배치 도구 유지/개선

**금지 사항**:
- 더 이상 quarter-view, perspective mesh floor, gym_floor_tileset_quarterview_v1 관련 작업 금지.
- 안정화된 입구/데스크/동선 영역 절대 수정 금지.

---

**문서 이력**
- 2026-04: 정면형 쿼터뷰 전환 실험 공식 포기 → 순수 탑뷰 복귀 결정. PROJECT_CONTEXT 전면 업데이트.
- 이전 버전: Quarter-View prototype 활성 기간 (PerspectiveGridFloorVisualizer, mesh floor 등)

이 문서를 모든 개발자가 최우선으로 참조할 것.
