---
title: "Markdown Chat Virtualization"
---

# Markdown Chat Virtualization

The `Markdown Chat` sample shows one of the highest-leverage patterns in this repository: exact-height-first virtualization.

Instead of measuring a live UI tree, the sample:

1. parses markdown once into reusable templates
2. prepares inline and code blocks with `Pretext`
3. builds a deterministic `ConversationFrame`
4. computes the visible range from scroll offset and occlusion chrome
5. materializes only the visible message window into Uno elements

## Why this matters

Traditional rich chat UIs often depend on UI-tree or DOM measurement after layout. That makes virtualization harder because height is discovered late.

The sample reverses that:

- message height is known before rendering
- total scroll extent is known before rendering
- scroll-window virtualization becomes a pure geometry problem

## Core APIs involved

| API | Role in the sample |
| --- | --- |
| `PrepareRichInline` | Prepared paragraph-like inline content for markdown text runs. |
| `MeasureRichInlineStats` | Predicted inline block height from line count and widest line. |
| `WalkRichInlineLineRanges` | Streamed visible inline lines during materialization. |
| `PrepareWithSegments(..., new PrepareOptions(WhiteSpaceMode.PreWrap))` | Prepared fenced code blocks. |
| `MeasureLineStats` | Predicted code-block line count and width. |
| `LayoutWithLines` | Materialized code-block lines once a visible message is being rendered. |

## Block model

The sample treats a chat message as a block container with a few leaf types:

- inline markdown paragraphs and headings
- fenced code blocks
- horizontal rules
- list and quote wrappers that shift content geometry and add markers or rails

This is the important architectural point: `Pretext` stays the paragraph leaf, while the sample owns the higher-level block layout rules.

## Files to read

- `samples/PretextSamples/Samples/MarkdownChatData.cs`
- `samples/PretextSamples/Samples/MarkdownChatModel.cs`
- `samples/PretextSamples/Samples/MarkdownChatSampleView.cs`

## Takeaway

If your application can convert rich content into deterministic block and inline templates, `Pretext` gives you the measurement pieces needed to virtualize large scroll surfaces without waiting for a live layout pass.
