# What Code Doesn't Remember

If you build software, you've probably had this experience at least once.

You open code you wrote six months ago and have no idea why you built it that way. You vaguely recall a heated debate on Slack, but that context is buried somewhere in the chat logs. The reason behind the decision is gone — only the outcome remains.

Code tells you **"what."** But it never tells you **"why."**

* * *

<!-- img: Team discussing design on Slack → six months later only code remains (Before/After contrast) -->

The age of AI-generated code is here. GitHub Copilot completes functions, and Claude generates entire modules. These tools can accelerate code writing, yet one thing has not changed.

**Recording and preserving agreements is still a human responsibility.**

AI can generate code, but it doesn't know "why this design was chosen," "who agreed to this decision," or "why Alternative B was rejected three weeks ago." The faster code generation becomes, the more devastating the absence of the agreements behind that code.

* * *

<!-- img: AI generates code quickly but "why?" remains blank -->

## A Recurring Pattern

This problem is not new.

At the 1968 NATO Software Engineering Conference, Edsger Dijkstra said:

> "When there were no machines, programming was no problem at all. When we had a few weak computers, programming became a mild problem. Now that we have gigantic computers, programming has become an equally gigantic problem."

As tools grow more powerful, complexity increases — and as complexity increases, **tracking "why we did it this way"** becomes the core challenge. Waterfall relied on documents, Agile relied on conversations, and now we face the same problem atop AI-generated code.

* * *

<!-- img: Timeline 1968 → 2000 → 2026. Each era's bottleneck converges on "preserving agreements" -->

## The Spec as a Memory Device

SDDP is one answer to this problem.

The core idea is simple. **Connect discussions, requirements, reviewed specs and implementation artifacts so decisions remain traceable.** A spec is not just a document — it is an Immutable Agreement Artifact where the team's consensus is permanently recorded.

Approval and locking are separate steps. Once a spec is locked, changes follow the versioning workflow rather than silently replacing the agreed content. Just as Git preserves the change history of code, SDDP preserves the **change history of decisions**.

* * *

![Spec status flow: Draft → InReview → Approved → Locked](assets/intro/04-spec-flow.gif)

## The Square

Drew Breunig argued in [The Spec-Driven Development Triangle](https://www.dbreunig.com/2026/03/04/the-spec-driven-development-triangle.html) that Spec, Test, and Code should form a circular feedback loop. Implementation improves the specification, the specification improves the tests, and the tests improve the code — a triangle.

SDDP adds one more vertex to this long-term design: **Conversation**. The diagram describes the intended feedback loop, not an automatic code-generation or rule-analysis feature of the free edition.

```
    Conversation
        ↓
    Requirement
        ↓
       Spec ←──── Rule (Drift Detection)
        ↓              ↑
      Code ────────────┘
```

Requirements emerge from conversations, requirements are refined into specs, and implementation artifacts link back to those specs. Automated generation and rule-based semantic verification are goals of the broader design. The starting point is **people's conversations**.

* * *

![SDDP Square: Conversation → Requirement → Spec → Code](assets/intro/05-sddp-square.gif)

## What the free v0.4.0 edition provides

The public application records conversations, requirements, specs, relationships and artifact
status. Approval and locking are explicit workflow steps. Artifact content-hash verification
is a file-consistency check, not proof that code satisfies the meaning of a spec; maintenance
operations also depend on deployment flags and permissions.

AI assistance, semantic search and code-generation tools are not included. The Generation tab
does not generate code. The animated examples illustrate the workflow and do not establish
that every part of the long-term feedback loop is implemented. See the [public guide](public-guide.md)
for the supported public workflow.

## People Decide, Specs Remember, Code Proves

Let's circle back to where we started.

What if, when you open code written six months ago, you could know "why it was built this way"? Which discussion led to that decision, who agreed, and which alternatives were rejected?

SDDP is a platform that preserves that "why."

Code keeps changing. The faster AI generates it, the faster it changes. But agreements — why this decision was made, in what context this design was chosen — must be preserved. Only then can you six months from now, or your successor, or an AI, understand that code and change it correctly.

> **"People decide, specs remember, code proves."**

* * *

![SDDP App Shell overview](assets/intro/06-app-shell.gif)

---

*SDDP is an open-source project. Check it out on [GitHub](https://github.com/yang-gyunam/sddp).*

<!--
  Image Placeholders (screenshot/illustration replacement guide)

  1. Before/After contrast — discussions disappearing
  2. Limits of AI code generation — "why?" is blank
  3. Timeline — 1968 → 2026, recurring problem of preserving agreements
  4. Spec status flow — Draft → InReview → Approved → Locked
  5. SDDP square — Conversation → Requirement → Spec → Code → Rule
  6. Full screen — App Shell screenshot
-->
