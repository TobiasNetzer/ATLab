# 10. Commands, Variables & Expressions

Command texts in ATLab are more than plain strings. Inside curly braces you can insert variables, encode binary data and calculate values. This chapter describes the syntax.

## Where the syntax applies

| Location | Variables `{Name}` | Encoding blocks `{enc:…}` | Expressions `{expr:…}` |
|----------|:---:|:---:|:---:|
| Script commands | ✔ (script + runtime) | ✔ | ✔ |
| Device Command | ✔ | ✔ | ✔ |
| Interface Command | ✔ | ✔ | ✔ |
| External Program | ✔ | text encodings only | ✔ |
| Comment / message text | ✔ | text encodings only | ✔ |
| Lower/Nominal/Upper limit, Delay | ✔ | – | ✔ |
| Expression evaluation source | ✔ | – | whole field is an expression |

## Variables

Write a variable name in curly braces:

```text
SOUR:VOLT {V_Supply}
```

Three kinds of variables can be used:

1. **Script variables** (only inside scripts) – defined in the script, value set per step.
2. **Runtime variables** – defined on the Configuration tab, available everywhere.
3. **System variables** – provided by ATLab during test execution, see below.

Script variables are replaced first. Names may contain letters, digits and `_`. A reference to an unknown variable is replaced with `0.0`, so check spelling carefully.

A variable can also hold text: *Assign Result to Variable* writes `Timeout`, `NACK` or `Overflow` when a step ends that way. Such a variable cannot be used where a number is needed. In the Expression evaluation source or a limit, nominal value or delay it is an execution error and **the test stops**, with a message that names the variable, e.g. *Variable 'Reg' holds 'NACK', which is not a number.* Handle these results with the Fail-Action of the step that produced them (e.g. *Repeat* or *Jump To*), before a later step uses the variable.

### System variables

System variables start with `sys.` and are read-only. They can be used everywhere runtime variables can, but they cannot be selected under *Assign Result to Variable*.

| Variable | Value |
|----------|-------|
| `{sys.SerialNumber}` | Serial number entered at test start (`-` if none) |
| `{sys.User}` | Operator (logged-in user name) |
| `{sys.ProjectName}` | Name of the project |
| `{sys.Year}` | Current year, four digits, e.g. `2026` |
| `{sys.Month}` | Current month, two digits, `01`–`12` |
| `{sys.Day}` | Current day, two digits, `01`–`31` |
| `{sys.Hour}` | Current hour, two digits, `00`–`23` |
| `{sys.Minute}` | Current minute, two digits, `00`–`59` |
| `{sys.Second}` | Current second, two digits, `00`–`59` |
| `{sys.ElapsedTime}` | Seconds since the test started, e.g. `12.34` |
| `{sys.ModuleName}` | Name of the module of the current step |
| `{sys.StepNumber}` | Number of the current step within its module |
| `{sys.FailedSteps}` | Number of failed steps so far |

The values are updated at the start and end of every step. `{sys.FailedSteps}` therefore does not yet include the current step while it is running (e.g. in its Pass/Fail comment).

Combine the date and time parts into any format you need:

```text
{sys.Day}.{sys.Month}.{sys.Year} {sys.Hour}:{sys.Minute}
```

Example – write the serial number to the DUT over UART: `SN={sys.SerialNumber}{hex:0A}`

## Encoding blocks

An encoding block has the form `{encoding:content}`. The content can itself contain variables.

| Block | Result | Example | Sent / produced |
|-------|--------|---------|-----------------|
| `{hex:…}` | Raw bytes from hexadecimal. Spaces, `-` and `0x` are ignored. | `{hex:0A 1F}` | bytes `0x0A 0x1F` |
| `{bin:…}` | Raw bytes from binary digits (padded to whole bytes). | `{bin:10100101}` | byte `0xA5` |
| `{ascii:…}` | Text as is. | `{ascii:ABC}` | `ABC` |
| `{upper:…}` | Text in upper case. | `{upper:{Mode}}` | `DC` |
| `{lower:…}` | Text in lower case. | `{lower:VOLT}` | `volt` |
| `{len:…}` | Length of the text. | `{len:HELLO}` | `5` |
| `{expr:…}` | Result of an arithmetic expression. | `{expr:{Range}*2}` | `200` |
| `{u16be:…}` etc. | Number as raw binary value, see [Binary numbers](#binary-numbers). | `{u16be:1000}` | bytes `0x03 0xE8` |

Text outside braces is sent as ASCII. Blocks can be mixed freely:

```text
WRITE{hex:00 FF}{Value}
```

Typical uses:

- Appending a line feed for an instrument that needs it: `MEAS?{hex:0A}`
- Sending a register address and value over I²C: `{hex:10}{hex:{RegValue}}`
- Writing a calculated value to a 16-bit register: `{hex:10}{u16be:{Setpoint}*10}`

> Raw byte blocks (`hex`, `bin` and the binary number blocks) are only meaningful for commands sent to devices and interfaces. In external program command lines, comments and limits the content is inserted as text (binary number blocks: the calculated number).

### Binary numbers

The blocks `{u8:…}`, `{i8:…}`, `{u16be:…}`, `{u16le:…}`, `{i16be:…}`, `{i16le:…}`, `{u32be:…}`, `{u32le:…}`, `{i32be:…}`, `{i32le:…}`, `{f32be:…}` and `{f32le:…}` convert a number into its raw binary representation:

| Part | Meaning |
|------|---------|
| `u` / `i` / `f` | unsigned integer / signed integer (two's complement) / 32-bit float (IEEE 754) |
| `8` / `16` / `32` | size in bits (1, 2 or 4 bytes); floats are always `32` |
| `be` / `le` | byte order: big-endian (most significant byte first) / little-endian. Without suffix: big-endian. |

The content is evaluated like an [expression](#expressions), so it can be a number, a variable, a hex literal or a calculation. Integers are rounded to the nearest whole number; a value outside the type's range (e.g. `{u8:256}` or `{u16be:-1}`) stops the step with an execution error.

| Block | Bytes sent |
|-------|------------|
| `{u16be:1000}` | `0x03 0xE8` |
| `{u16le:1000}` | `0xE8 0x03` |
| `{i8:-1}` | `0xFF` |
| `{i16le:-300}` | `0xD4 0xFE` |
| `{u32be:0x12345678}` | `0x12 0x34 0x56 0x78` |
| `{u16be:{Setpoint}*10}` with `Setpoint` = `100` | `0x03 0xE8` |
| `{f32be:3.3}` | `0x40 0x53 0x33 0x33` |

The counterpart for reading binary answers is `{byte:N}` / `{f32}` in [Response Processing](11-response-processing.md#numeric-extraction).

## Expressions

An expression block `{expr:…}` — or the whole field of an **Expression** step — is evaluated arithmetically.

Operators, from the most tightly binding to the least — the same order as in C# (and C, Java, JavaScript):

| Operator | Meaning |
|----------|---------|
| `( )` | Grouping |
| `-x` `+x` | Sign (unary minus / plus) |
| `*` `/` `%` | Multiply, divide, remainder |
| `+` `-` | Add, subtract |
| `<<` `>>` | Bit shift left/right (integers) |
| `&` | Bitwise AND (integers) |
| `^` | Bitwise XOR (integers) |
| `\|` | Bitwise OR (integers) |

Operators of the same level are evaluated left to right. For example `1 + 2 << 1` is `(1 + 2) << 1 = 6`, and `1 | 2 * 3` is `1 | (2 * 3) = 7`. Parentheses still make mixed bitwise/arithmetic expressions easier to read.

Numbers can be written as decimal (`3.3`), scientific (`1.5e-3`) or hexadecimal (`0x3F`). Both `.` and `,` are accepted as decimal separator, regardless of the language setting (`1,5` and `1.5` are both 1.5).

Negative numbers work as expected: `-5`, `2 * -3`, `-(1 + 2)` and `1 - -0.3` are all valid. This also covers variables holding negative values — if `{Offset}` is `-0.3`, the expression `1 + {Offset}` becomes `1 + -0.3 = 0.7`.

Examples:

```text
{expr:{V_Out} / {V_In} * 100}       → efficiency in %
{expr:({Raw} >> 4) & 0x0F}          → upper nibble of a register
{expr:{Count} + 1}                  → increment (assign result to Count)
{expr:{V_Meas} - {Offset}}          → offset correction, works with negative offsets
```

The result of an `{expr:…}` block is inserted with up to 15 significant digits, so floating-point noise does not end up in commands: `{expr:0.1+0.2}` becomes `0.3`, not `0.30000000000000004`.

If an expression inside a command cannot be evaluated, the block is left unchanged. In an **Expression** step, an invalid expression is an execution error.

## Units and SI prefixes

Wherever ATLab parses a number entered by you (limits, nominal, delay), the following suffixes are accepted:

| Suffix | Factor |
|--------|--------|
| `p` | 10⁻¹² |
| `n` | 10⁻⁹ |
| `u` | 10⁻⁶ |
| `m` | 10⁻³ |
| `k` | 10³ |
| `M` | 10⁶ |

The step's unit may follow the number (`4.7kOhm` with unit `Ohm`). For the delay, the unit is always seconds (`250ms`).

Measured values are displayed with the most suitable prefix. Wrap the unit in braces (e.g. `{rpm}`) to show raw numbers without prefixes.

## Putting it together

A script command:

```text
CONF:VOLT:DC {Range},{expr:{Range}/1000}
```

with script variable `Range = 10` is sent as:

```text
CONF:VOLT:DC 10,0.01
```

A Comment:

```text
Measured supply was {V_Supply} V. Please confirm the display shows the same value.
```

is shown to the operator with the current value of `V_Supply` inserted.
