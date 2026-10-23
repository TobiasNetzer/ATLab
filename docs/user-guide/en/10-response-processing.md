# 10. Response Processing

Instruments and DUTs do not always answer with a bare number. A DMM might answer `+1.2001555E+01` (fine), but a DUT might send `ID:42;OK\r\n`, a binary frame, or a status register. **Response Processing** turns such raw answers into the single number that the step evaluates.

The **Response Processing** panel is available for the evaluation sources **Script**, **Device Command**, **Interface Command**, **File** and **User Input**.

## Without custom processing

If **Enable Custom Response Processing** is off, the raw answer is interpreted as ASCII text and converted to a number. Leading/trailing white space (including the line terminator) is ignored. Anything else that is not a number causes an execution error.

## With custom processing

Enable **Enable Custom Response Processing** and fill in:

| Field | Description |
|-------|-------------|
| **Skip** | Number of bytes to discard from the start of the answer. |
| **Length** | Number of bytes to keep after skipping (`0` = all remaining bytes). If fewer bytes are left, the step reports an execution error. |
| **Mask** | Rules that the remaining bytes must match, and/or how to convert them to a number. |
| **Display Mode** | `ASCII` or `HEX` – how the received data is shown in the panel. |

Processing order:

1. **Skip** and **Length** cut out the relevant part of the answer.
2. The **match rules** of the mask are checked against that part.
3. If all match rules are satisfied, the **numeric extraction** (if any) converts that part into a number.

### Received sequence

After the step has run, the panel's **Received Sequence** area shows:

- **Original:** the complete raw answer,
- **Processed:** the part left after Skip/Length,
- **Result:** the value passed on to limit evaluation.

Run the step with **Execute single step** (`F3`) while designing a mask to see these values immediately. Switch **Display Mode** to `HEX` for binary protocols.

## Mask syntax

A mask is a sequence of elements separated by spaces.

### Match rules

| Element | Matches | Example |
|---------|---------|---------|
| `"text"` | The ASCII text appears in the data | `"OK"` |
| `0x…` | The byte sequence appears in the data | `0xAA55` |

Several match rules must all be found. Text rules are searched **in the given order**, e.g. `"ID:" ";OK"` requires `ID:` followed later by `;OK`; the same applies among byte-sequence rules. (Text and byte rules are tracked separately, so the order between a text rule and a byte rule is not enforced.)

### Numeric extraction

| Element | Converts the processed bytes as … | Example data → result |
|---------|------------------------------------|-----------------------|
| `{float}` | ASCII decimal number with optional fraction/exponent | `3.274` → 3.274 |
| `{dec}` | ASCII integer | `42` → 42 |
| `{hex}` | ASCII hexadecimal digits | `1F` → 31 |
| `{bin}` | ASCII binary digits | `1010` → 10 |
| `{ascii}` | ASCII characters combined into an integer (big-endian) | `AB` → 16706 |
| `{byte:N}` | the first *N* raw bytes as an unsigned integer, most significant byte first | `0x01 0x2C` with `{byte:2}` → 300 |
| `{byte:N,lsb}` | the first *N* raw bytes, least significant byte first | `0x2C 0x01` with `{byte:2,lsb}` → 300 |
| `{byte:N,msb}` | same as `{byte:N}` | |

Only one numeric extraction is used per mask. **The extraction always works on the complete processed part** (after Skip/Length), not on the text following a match rule — use Skip and Length to isolate the number.

#### Match rules check, Skip/Length cut

Match rules and numeric extraction do two independent jobs: the match rules only decide *whether* the data is acceptable; they do not move the starting point of the extraction. Example — a DUT answers `Result=3.27\r\n`:

| Mask | Skip | Length | What happens | Result |
|------|------|--------|--------------|--------|
| `"Result=" {float}` | 0 | 0 | `Result=` is found ✔, then `{float}` tries to parse the whole text `Result=3.27\r\n` ✘ | execution error |
| `{float}` | 7 | 0 | Skip removes `Result=`, `{float}` parses `3.27\r\n` ✔ | `3.27` |
| `"3."` | 7 | 0 | Skip removes `Result=`, `3.` is found ✔, no extraction | `1` |

So whenever a number has a prefix, remove the prefix with **Skip** (and a suffix with **Length**) rather than matching it. This works as long as the number starts at a fixed position — its own length may vary (`Result=3.27` and `Result=12.5` both work with Skip `7`, Length `0`). If the prefix itself varies in length (e.g. `CH1:3.27` vs. `CH12:3.27`), have the DUT/instrument send a fixed-width format, or let a small script (Shell Command step) write the bare value to a file and evaluate it with a File step.

### Result rules

| Mask content | Data matches all rules | Data does not match |
|--------------|------------------------|---------------------|
| Match rules only | `1` | `0` |
| Numeric extraction only | the extracted number | – |
| Match rules + extraction | the extracted number | `0` |
| *(empty mask)* | `0` | `0` |

The step reports an **execution error** (instead of a result) if

- the answer is shorter than **Skip** + **Length** (with Length > 0), e.g. a truncated frame,
- no data is left after **Skip**, or the answer is empty,
- the numeric extraction fails, e.g. `{dec}` on `4.2`, or `{byte:2}` on a single byte,
- the mask is invalid, e.g. an unknown extraction such as `{foo}`.

The error message names the cause, e.g. *Response too short: expected 5 bytes after skipping 2, but only 3 available*.

## Examples

**1. Check for an acknowledge**

DUT answers `ACK\r\n` or `NAK\r\n`.

| Field | Value |
|-------|-------|
| Mask | `"ACK"` |
| Limits | `1 … 1` |

**2. Value at a fixed position**

DUT answers `T=023.5C\r\n`.

| Field | Value |
|-------|-------|
| Skip | `2` |
| Length | `5` |
| Mask | `{float}` |
| Result | `23.5` |

**3. 16-bit register over I²C**

Interface Command, I²C, **Read Bytes** `2`, sensor sends `0x0C 0xE4`.

| Field | Value |
|-------|-------|
| Mask | `{byte:2}` |
| Result | `3300` |

Scale it with a following Expression step, e.g. `{Raw} * 0.001` (with *Assign Result to Variable* = `Raw` on the first step).

**4. Binary frame with header**

Answer `0xAA 0x55 0x03 0xE8`: two header bytes, then a big-endian value.

| Field | Value |
|-------|-------|
| Skip | `0` |
| Mask | `0xAA55` |

checks the header → result `1`. To read the value instead, use Skip `2`, Mask `{byte:2}` → `1000`.

**5. Status bit**

Register read gives one byte; bit 3 must be set. Read it with `{byte:1}`, assign to variable `Status`, then use an Expression step `({Status} >> 3) & 1` with limits `1 … 1`.
