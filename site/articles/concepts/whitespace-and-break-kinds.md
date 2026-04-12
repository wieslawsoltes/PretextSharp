---
title: "Whitespace and Break Kinds"
---

# Whitespace and Break Kinds

`Pretext` does not treat all separators the same. Segment kind matters because it changes where lines may break and what text remains visible at line edges.

## White space modes

| Mode | Behavior |
| --- | --- |
| `WhiteSpaceMode.Normal` | Ordinary spaces, tabs, line breaks, and form feeds collapse like normal web-style wrapping. Leading and trailing collapsible whitespace is removed. |
| `WhiteSpaceMode.PreWrap` | Ordinary spaces are preserved, hard breaks stay explicit, tabs stay explicit, and whitespace-only lines remain visible. |

## Segment break kinds

| Kind | Meaning | Visible at line end? | Break behavior |
| --- | --- | --- | --- |
| `Text` | Normal text run | Yes | May wrap internally when the run is breakable |
| `Space` | Ordinary collapsible space | No | Break opportunity after the space |
| `PreservedSpace` | Preserved space in `PreWrap` | Yes | Break opportunity after the space |
| `Tab` | Explicit tab in `PreWrap` | Yes | Break opportunity after the tab |
| `Glue` | Non-breaking separator such as NBSP or word joiner | Yes | Prevents a break between surrounding content |
| `ZeroWidthBreak` | Explicit soft break opportunity | No | Break opportunity without visible width |
| `SoftHyphen` | Discretionary hyphen point | Only when used | Adds the discretionary hyphen when a break is taken there |
| `HardBreak` | Forced line boundary | No | Ends the current line immediately |

## Important behaviors

- Ordinary whitespace collapses in `WhiteSpaceMode.Normal`
- Spaces and tabs are preserved in `WhiteSpaceMode.PreWrap`
- Hard breaks remain explicit line boundaries in `PreWrap`
- Non-breaking spaces stay glued to surrounding content
- Zero-width spaces create explicit break opportunities
- Soft hyphens can introduce a discretionary hyphen when a break is taken

## Hanging whitespace

In `PreWrap`, trailing spaces and tabs remain part of the visible line content. That is why the test suite checks:

- hanging spaces at line end
- trailing spaces before a hard break
- tabs aligned to tab stops
- consecutive tabs producing distinct tab advances
- whitespace-only lines between hard breaks

## Why this matters

These rules make the engine useful for UI that needs more than simple `TextBlock` wrapping. The tests cover preserved spaces, tabs, empty lines, trailing spaces before hard breaks, and discretionary hyphen insertion.

## Supported special cases

The current parity tests cover:

- non-breaking spaces and narrow no-break spaces
- word joiners
- zero-width spaces
- discretionary soft hyphens
- opening and closing quote attachment
- URL-like runs and punctuation chains
- Arabic, Devanagari, Myanmar, CJK, Hangul, and astral CJK punctuation behavior

Those cases are the practical behavior contract for the current implementation.
