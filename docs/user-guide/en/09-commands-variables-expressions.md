# 9. Commands, Variables & Expressions

Command texts in ATLab are more than plain strings. Inside curly braces you can insert variables, encode binary data and calculate values. This chapter describes the syntax.

## Where the syntax applies

| Location | Variables `{Name}` | Encoding blocks `{enc:…}` | Expressions `{expr:…}` |
|----------|:---:|:---:|:---:|
| Script commands | ✔ (script + runtime) | ✔ | ✔ |
| Device Command | ✔ | ✔ | ✔ |
| Interface Command | ✔ | ✔ | ✔ |
| Shell Command | ✔ | text encodings only | ✔ |
| Comment / message text | ✔ | text encodings only | ✔ |
| Lower/Nominal/Upper limit, Delay | ✔ | – | ✔ |
| Expression evaluation source | ✔ | – | whole field is an expression |

## Variables

Write a variable name in curly braces:

```text
SOUR:VOLT {V_Supply}
```

Two kinds of variables can be used:

1. **Script variables** (only inside scripts) – defined in the script, value set per step.
2. **Runtime variables** – defined on the Configuration tab, available everywhere.

Script variables are replaced first. Names may contain letters, digits and `_`. A reference to an unknown variable is replaced with `0.0`, so check spelling carefully.

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
| `{bytes:…}` | UTF-8 byte values as a list. | `{bytes:AB}` | `[65, 66]` |
| `{expr:…}` | Result of an arithmetic expression. | `{expr:{Range}*2}` | `200` |

Text outside braces is sent as ASCII. Blocks can be mixed freely:

```text
WRITE{hex:00 FF}{Value}
```

Typical uses:

- Appending a line feed for an instrument that needs it: `MEAS?{hex:0A}`
- Sending a register address and value over I²C: `{hex:10}{hex:{RegValue}}`

> Raw byte blocks (`hex`, `bin`) are only meaningful for commands sent to devices and interfaces. In shell commands, comments and limits the content is inserted as text.

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

Numbers can be written as decimal (`3.3`), scientific (`1.5e-3`) or hexadecimal (`0x3F`). Both `.` and the current culture's decimal separator are accepted.

Negative numbers work as expected: `-5`, `2 * -3`, `-(1 + 2)` and `1 - -0.3` are all valid. This also covers variables holding negative values — if `{Offset}` is `-0.3`, the expression `1 + {Offset}` becomes `1 + -0.3 = 0.7`.

Examples:

```text
{expr:{V_Out} / {V_In} * 100}       → efficiency in %
{expr:({Raw} >> 4) & 0x0F}          → upper nibble of a register
{expr:{Count} + 1}                  → increment (assign result to Count)
{expr:{V_Meas} - {Offset}}          → offset correction, works with negative offsets
```

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
