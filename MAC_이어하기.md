# Mac에서 이어서 작업하기

이 저장소에는 Unity 프로젝트 원본과 현재까지의 수정 사항이 저장되어 있습니다.
Unity 버전은 `ProjectSettings/ProjectVersion.txt` 기준 **6000.6.6f1**입니다.

## 프로젝트 가져오기

1. Mac에 Unity Hub를 설치하고 로그인합니다.
2. Unity Hub에서 **6000.6.6f1** 에디터를 설치합니다. 설치 시 Mac의 프로세서에 맞는 에디터를 선택합니다.
3. https://github.com/qkswh123/- 에서 **Code → Download ZIP**으로 받은 뒤 압축을 풉니다.
4. Unity Hub의 프로젝트 추가 메뉴에서 `Assets`, `Packages`, `ProjectSettings`가 들어 있는 폴더를 선택합니다.
5. 처음 열 때 패키지와 에셋 임포트가 끝날 때까지 기다립니다.
6. `Assets/Scenes/Level1.unity`를 열고 Play를 누릅니다. Game 창을 클릭한 뒤 좌우 방향키로 이동합니다.

Git으로 계속 저장하려면 ZIP 다운로드 대신 터미널에서 다음을 실행해도 됩니다.

```sh
git clone https://github.com/qkswh123/-.git penguin-climb
cd penguin-climb
```

이미 이 저장소를 복제한 Mac에서는 로컬 수정 사항을 먼저 저장한 다음 `git pull --ff-only`로 최신 변경을 받습니다.

`Library`, `Temp`, `Logs`, `UserSettings`는 Mac의 Unity가 새로 생성합니다. Windows에서 이 폴더를 복사할 필요가 없습니다.
캐릭터·블록·코인 원본과 `.meta` 파일은 저장소에 포함되어 있습니다.

## 저장된 최신 게임 상태

- 레벨 1~20마다 다른 캐릭터와 3D 배경, 통일된 블록 모양·색상.
- 레벨 1은 펭귄과 빙하 발판. 50칸부터 레벨마다 50칸씩 증가.
- 모든 등반 발판에 회전하는 3D 코인. 정상 착지마다 **10포인트** 획득.
- 포인트 창은 다른 안내창과 같은 **반투명 배경**, 화면 **왼쪽 아래**에 표시.
- 재시작이나 레벨 변경 시 해당 레벨 포인트는 0점부터 시작.
- 장애물 본체 고정 크기, 영구 윤곽 링, 화면 등장 즉시 `!` 표시. 코인은 장애물과 좌우로 분리.
- Windows와 Mac의 한글 시스템 폰트를 순서대로 찾도록 UI 폰트 설정 보완.

## 다음에 확인할 작업

Windows에서 코인 10포인트 누적, 중복 획득 방지, 실패 착지 무보상, 재사용 발판과 레벨1 완료 500점 검증은 통과했습니다.
20개 캐릭터의 대기·점프·착지도 확인했습니다.

다만 최신 장애물 윤곽 수정 이후의 **1~20레벨 전체 반복 검사**, 최신 포인트 창의 **실제 화면 확인**, **Mac 실행 확인**은 남아 있습니다.
Windows에서 Unity가 종료된 뒤 라이선스 초기화 오류가 발생하여 최종 검사를 이어가지 못했습니다.
Mac에서는 Unity Hub 로그인·라이선스 활성화 상태를 확인한 뒤 먼저 레벨1을 실행해 주세요.

주요 코드:

- `Assets/Scripts/ClimbLevel.cs`: 포인트와 UI 위치
- `Assets/Scripts/StepCoin.cs`: 코인 표시와 획득 효과
- `Assets/Scripts/StepHazard.cs`: 장애물 표시와 안전/위험 판정
- `Assets/Scripts/IceStaircase.cs`: 계단·코인·장애물 생성과 재사용
- `Assets/GameData/ClimbGameSettings.asset`: 캐릭터 배정

검증 코드:

- `Assets/Tests/ClimbCoinVerification.cs`
- `Assets/Tests/ClimbAllHazardsVerification.cs`
- `Assets/Tests/ClimbRosterVerification.cs`
- `Assets/Tests/ClimbVisualVerification.cs`

이 코드는 Unity 편집기 전용 검증 도우미이며 저장된 게임 씬에는 테스트 오브젝트가 포함되지 않습니다.

## Mac의 Codex에서 이어갈 때 전달할 내용

> MAC_이어하기.md와 검증_현황.txt를 읽고 현재 상태에서 이어서 작업해 줘.
> 레벨1부터 실행해 왼쪽 아래 반투명 포인트 창과 코인당 10점 획득을 확인하고,
> 레벨1~20에서 장애물이 배경에 묻히거나 갑자기 나타나는 문제가 없는지 검증해 줘.
> 기존 배경, 레벨별로 통일된 블록, 연한 회색 테두리, 캐릭터 배정과 계단 수 규칙은 유지해 줘.
