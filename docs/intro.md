# What Code Doesn't Remember

If you build software, you've probably had this experience at least once.

You open code you wrote six months ago and have no idea why you built it that way. You vaguely recall a heated debate on Slack, but that context is buried somewhere in the chat logs. The reason behind the decision is gone — only the outcome remains.

Code tells you **"what."** But it never tells you **"why."**

* * *

<!-- img: Team discussing design on Slack → six months later only code remains (Before/After contrast) -->

The age of AI-generated code is here. GitHub Copilot completes functions, and Claude generates entire modules. Code writing speed has increased tenfold, yet one thing hasn't changed.

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

The core idea is simple. **Transform discussions into structured specs, generate code from those specs, and track every change.** A spec is not just a document — it is an Immutable Agreement Artifact where the team's consensus is permanently recorded.

Approved specs are locked. When changes are needed, you don't modify the existing spec — you create a new version. Just as Git preserves the change history of code, SDDP preserves the **change history of decisions**.

* * *

![Spec status flow: Draft → InReview → Approved → Locked](assets/intro/04-spec-flow.gif)

## The Square

Drew Breunig argued in [The Spec-Driven Development Triangle](https://www.dbreunig.com/2026/03/04/the-spec-driven-development-triangle.html) that Spec, Test, and Code should form a circular feedback loop. Implementation improves the specification, the specification improves the tests, and the tests improve the code — a triangle.

SDDP adds one more vertex to this triangle: **Conversation**.

```
    Conversation
        ↓
    Requirement
        ↓
       Spec ←──── Rule (Drift Detection)
        ↓              ↑
      Code ────────────┘
```

Requirements emerge from conversations, requirements are refined into specs, and code is generated from specs. When code drifts from the spec, a Rule detects it. It's not a triangle — it's a square. And the starting point of this square is not code, but **people's conversations**.

* * *

![SDDP Square: Conversation → Requirement → Spec → Code](assets/intro/05-sddp-square.gif)

## People Decide, Specs Remember, Code Proves

Let's circle back to where we started.

What if, when you open code written six months ago, you could know "why it was built this way"? Which discussion led to that decision, who agreed, and which alternatives were rejected?

SDDP is a platform that preserves that "why."

Code keeps changing. The faster AI generates it, the faster it changes. But agreements — why this decision was made, in what context this design was chosen — must be preserved. Only then can you six months from now, or your successor, or an AI, understand that code and change it correctly.

> **"People decide, specs remember, code proves."**

* * *

![SDDP App Shell overview](assets/intro/06-app-shell.gif)

---

*SDDP is an open-source project. Check it out on [GitHub](https://github.com/user/sddp).*

<!--
  Image Placeholders (screenshot/illustration replacement guide)

  1. Before/After contrast — discussions disappearing
  2. Limits of AI code generation — "why?" is blank
  3. Timeline — 1968 → 2026, recurring problem of preserving agreements
  4. Spec status flow — Draft → InReview → Approved → Locked
  5. SDDP square — Conversation → Requirement → Spec → Code → Rule
  6. Full screen — App Shell screenshot
-->
