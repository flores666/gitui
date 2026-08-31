# AGENTS.md

## Core Rule
- Make the smallest correct change that fully satisfies the task.
- Prefer a 10-line fix over a 100-line redesign when both solve the problem equally well.
- Every added line should have a concrete reason to exist.
- Do not write code for hypothetical future requirements.
- Do not overengineer.
- Do not improve unrelated code while completing a focused task.

## Code Quality
- Write production-quality code intended to be read and maintained by humans.
- Prefer clear, explicit, predictable code over clever or overly generic code.
- Follow modern language, framework, and ecosystem practices.
- Apply SOLID, DRY, KISS, YAGNI and separation of concerns pragmatically, not mechanically.
- Optimize for readability, correctness, maintainability, and consistency with the existing codebase.
- Preserve domain terminology already used by the project.
- Avoid hidden behavior, surprising side effects, unnecessary indirection, and excessive nesting.
- Prefer simple control flow and early returns where they improve readability.
- Comments should explain non-obvious intent or constraints, not restate the code.

## Reuse Existing Code
- Prefer reusing or adapting existing code over writing new code from scratch.
- Before creating a new type, method, helper, abstraction, utility, validation rule, mapping, or workflow, check whether an equivalent already exists.
- Extend existing mechanisms when they are suitable for the task.
- Adapt existing code when a small change can make it reusable without making it less clear or more coupled.
- Do not duplicate logic that already has an appropriate source of truth.
- Do not create a parallel implementation of functionality already present in the project.
- Reuse is not mandatory when the existing code is architecturally inappropriate, unsafe, obsolete, excessively coupled, or would violate current development principles.
- Do not preserve a bad abstraction purely to maximize reuse.
- If adapting existing code would make it significantly more complex than a small focused implementation, prefer the simpler maintainable solution.

## Architecture and Design Patterns
- First understand the architecture and patterns already used by the project.
- Reuse existing architectural boundaries, conventions, abstractions, and design patterns.
- Use design patterns only when they solve a concrete current problem.
- Do not introduce a pattern merely because it is considered a best practice.
- Do not create interfaces, factories, strategies, builders, repositories, wrappers, mediators, handlers, or additional layers without a concrete need.
- Prefer extending an existing pattern over introducing a competing abstraction.
- Do not introduce a new architectural pattern for a local change when direct code is sufficient.
- If an abstraction increases code without meaningfully reducing complexity, coupling, duplication, or risk, do not introduce it.

## Scope
- Read only files relevant to the requested change.
- Use search before opening large parts of the repository.
- Trace only the execution flow necessary to understand the task.
- Touch the minimum number of files necessary.
- Do not modify unrelated modules.
- Do not rename, move, reformat, reorder, or clean up unrelated code.
- Do not change public APIs unless the task requires it.

## Implementation
- Patch existing code before creating new infrastructure.
- Reuse existing methods, types, constants, utilities, and abstractions whenever appropriate.
- Prefer direct implementation for simple behavior.
- Extract code only when extraction improves readability, reuse, testability, or separation of responsibilities.
- Do not create a helper for trivial one-use logic.
- Do not create an interface for a single implementation unless a real architectural boundary or testing requirement justifies it.
- Do not add configurability or extension points that are not currently required.
- Do not add fallback behavior or defensive logic for unsupported hypothetical scenarios.
- Preserve existing behavior outside the requested change.

## C#
- Follow existing repository conventions first.
- Use modern idiomatic C# supported by the project's target framework.
- Use nullable reference types correctly.
- Use explicit access modifiers.
- Use `sealed` unless inheritance is intentional.
- Prefer immutable state where practical.
- Keep methods and types focused, but do not fragment simple logic into unnecessary methods or classes.
- Avoid broad `catch` blocks.
- Do not swallow exceptions.
- Propagate `CancellationToken` through async operations where appropriate.
- Avoid sync-over-async.
- Dispose resources correctly.
- Avoid unnecessary allocations in performance-sensitive paths.
- Avoid magic strings and numbers when an existing constant, enum, option, or domain type should be used.
- Validate at appropriate system boundaries rather than duplicating validation everywhere.

## Workflow
1. Find the exact code responsible for the requested behavior.
2. Understand the existing architecture and conventions around it.
3. Search for existing code that can be reused or adapted.
4. Identify the root cause or exact required behavior change.
5. Determine the smallest correct implementation.
6. Implement it using existing code and patterns where appropriate.
7. Review `git diff`.
8. Remove unnecessary changes, abstractions, duplication, and speculative code.
9. Run the narrowest relevant build and tests.
10. Expand validation only when justified.

## Diff Discipline
Before finishing, inspect the diff and ask:

- Is every changed line required for this task?
- Can the same result be achieved more simply?
- Could existing code have been reused or adapted?
- Did I duplicate existing logic?
- Did I create a parallel mechanism unnecessarily?
- Did I add an abstraction without a concrete benefit?
- Did I introduce a design pattern unnecessarily?
- Did I modify unrelated code?
- Did I add code for hypothetical future requirements?
- Is the resulting code easy for another developer to understand?

If any answer indicates unnecessary complexity, simplify the patch.

## Efficiency
- Do not repeatedly reread unchanged files.
- Do not inspect unrelated parts of the repository without a concrete reason.
- Do not summarize files merely because they were inspected.
- Keep plans and progress messages concise.
- Do not repeat the task requirements.
- Do not explain obvious code.
- Prefer targeted builds and tests during development.
- Do not spend tokens designing abstractions that the current task does not require.

## Validation
- Check relevant regressions, edge cases, nullable behavior, API contracts, async/cancellation behavior, resource lifetime, and error handling.
- Add or update tests when behavior changes and the project has an established testing pattern.
- Do not create excessive tests for trivial implementation details.
- Test observable behavior rather than internal implementation whenever practical.
- Never claim a command, build, or test passed unless it was actually executed.
