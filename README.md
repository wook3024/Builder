# Protocol Code Generator

XML 프로토콜 정의 파일을 C++ 및 C# 코드로 자동 생성하는 도구입니다.

## 기능

- XML 기반 프로토콜 정의
- C++ 헤더 파일 생성 (.h)
- C# 코드 생성 (.cs)
- ClangSharp를 이용한 C++ 코드 분석
- C++ 타입을 C# 타입으로 자동 매핑
- typedef, enum, struct 지원
- 컬렉션 타입 지원 (std::vector, std::array 등)

## 요구사항

- .NET 10.0 SDK
- Visual Studio 2022 이상 (권장)

## 빌드

```bash
dotnet restore
dotnet build
```

## 사용법

### 1. XML 프로토콜 정의 작성

`samples/sample_protocol.xml` 참조:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<protocol name="GameProtocol" namespace="GameProtocol">
    <enum name="PacketType" type="uint16_t">
        <value name="Login" value="1" />
        <value name="Logout" value="2" />
    </enum>

    <message name="LoginRequest" id="1">
        <field name="username" type="string" />
        <field name="password" type="string" />
    </message>
</protocol>
```

### 2. 코드 생성 실행

```bash
cd src/ProtocolGenerator.CLI
dotnet run ../../samples/sample_protocol.xml ../../output
```

또는 빌드 후:

```bash
ProtocolGenerator.CLI.exe samples/sample_protocol.xml output
```

### 3. 생성된 파일 확인

- `output/GameProtocol.h` - C++ 헤더 파일
- `output/GameProtocol.cs` - C# 소스 파일

## 프로젝트 구조

```
ProtocolGenerator/
├── src/
│   ├── ProtocolGenerator.Core/      # 핵심 라이브러리
│   │   ├── Models/                  # 데이터 모델
│   │   ├── Parsers/                 # XML, C++ 파서
│   │   ├── Mappers/                 # 타입 매핑
│   │   └── Generators/              # 코드 생성기
│   └── ProtocolGenerator.CLI/       # CLI 도구
├── templates/                       # Scriban 템플릿
├── config/                          # 설정 파일
├── samples/                         # 샘플 프로토콜
└── output/                          # 생성된 코드 (자동 생성)
```

## XML 스키마

### Protocol 요소

```xml
<protocol name="프로토콜명" namespace="네임스페이스">
  <!-- 내용 -->
</protocol>
```

### Enum 정의

```xml
<enum name="ErrorCode" type="int32_t">
  <value name="Success" value="0" />
  <value name="Failed" value="1" />
</enum>
```

### Message 정의

```xml
<message name="메시지명" id="고유ID">
  <field name="필드명" type="타입" />
  <field name="배열필드" type="타입[]" size="크기" />
</message>
```

### 지원 타입

#### 기본 타입
- `int8_t`, `uint8_t`
- `int16_t`, `uint16_t`
- `int32_t`, `uint32_t`
- `int64_t`, `uint64_t`
- `float`, `double`
- `bool`
- `string`

#### 배열 타입
- `타입[]` - 가변 길이 배열 (std::vector)
- `타입[]` + `size` 속성 - 고정 길이 배열 (std::array)

## C++ ↔ C# 타입 매핑

| C++ Type | C# Type |
|----------|---------|
| int8_t | sbyte |
| uint8_t | byte |
| int16_t | short |
| uint16_t | ushort |
| int32_t | int |
| uint32_t | uint |
| int64_t | long |
| uint64_t | ulong |
| float | float |
| double | double |
| bool | bool |
| std::string | string |
| std::vector&lt;T&gt; | List&lt;T&gt; |
| std::array&lt;T,N&gt; | T[] |

## 예제

### 입력 (XML)

```xml
<message name="PlayerInfo" id="10">
  <field name="playerId" type="uint64_t" />
  <field name="playerName" type="string" />
  <field name="level" type="uint32_t" />
  <field name="health" type="float" />
</message>
```

### 출력 (C++)

```cpp
// Message ID: 10
struct PlayerInfo
{
    uint64_t playerId;
    std::string playerName;
    uint32_t level;
    float health;
};
```

### 출력 (C#)

```csharp
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PlayerInfo
{
    public ulong playerId;
    [MarshalAs(UnmanagedType.LPStr)]
    public string playerName;
    public uint level;
    public float health;
}
```

## 커스터마이징

### 템플릿 수정

`templates/` 디렉토리의 Scriban 템플릿을 수정하여 생성 코드 형식을 변경할 수 있습니다:

- `cpp_header.scriban` - C++ 헤더 파일 구조
- `cpp_struct.scriban` - C++ 구조체 템플릿
- `cpp_enum.scriban` - C++ enum 템플릿
- `csharp_namespace.scriban` - C# 네임스페이스 구조
- `csharp_struct.scriban` - C# 구조체 템플릿
- `csharp_enum.scriban` - C# enum 템플릿

### 타입 매핑 추가

`src/ProtocolGenerator.Core/Mappers/TypeMapper.cs`에서 새로운 타입 매핑을 추가할 수 있습니다.

## 라이선스

MIT License
# Builder
