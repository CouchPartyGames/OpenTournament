# Coding Standards

The Go coding standards for Open Tournament. They condense the
[Google Go Style Guide](https://google.github.io/styleguide/go/guide) and
[Google Go Best Practices](https://google.github.io/styleguide/go/best-practices),
and replace Google-internal tooling (Bazel, glog, Code Search) with its open-source equivalent.

When this document says nothing on a point, follow
[Effective Go](https://go.dev/doc/effective_go) and the Google
[Style Decisions](https://google.github.io/styleguide/go/decisions), in that order.

Keywords: **must** is a hard rule that review will block on. **Should** is the default; deviate only with a reason, stated in a comment or the PR. **Prefer** marks the better of several acceptable options.

---

## 1. Principles

Readable code has these attributes, **in order of importance**. When two conflict, the higher one wins.

1. **Clarity.** The reader can tell *what* the code does and *why*. Optimise for the reader, not the writer.
2. **Simplicity.** The goal is reached in the simplest way. Complexity is added deliberately and documented.
3. **Concision.** High signal-to-noise: no repetition, extraneous syntax, opaque names or needless abstraction.
4. **Maintainability.** A future programmer can change it correctly. APIs grow gracefully, assumptions are explicit, and unused features are absent.
5. **Consistency.** It looks like similar code in the same package, then the rest of this repo, then the wider Go ecosystem. Consistency breaks ties; it never overrides the principles above it.

### 1.1 Clarity

- Make the *what* obvious through names, structure, whitespace and small functions.
- Comment the *why*, especially for language subtleties (e.g. a closure capturing a variable far away) and business rules (e.g. why a Forfeit is scored differently in free-for-all).
- Don't write comments that restate the code, contradict it, or will rot. Prefer self-describing names.
- Code that uses an unusual pattern signals "pay attention here". Don't spend that signal on code that doesn't need it.

### 1.2 Simplicity and least mechanism

Simple code reads top to bottom, doesn't make the reader memorise earlier code, has no unnecessary layers, makes the flow of values and decisions visible, and has useful errors and test failures. It is often not "clever" code.

When several tools express the same idea, use the most standard one:

1. A core language construct (slice, map, struct, channel, loop).
2. The standard library.
3. Only then a well-established third-party module. Every new dependency must be justified in the PR.

```go
// Good: a map is a set.
seen := map[ParticipantID]bool{}

// Bad: pulling in a set library for membership checks only.
seen := sets.New[ParticipantID]()
```

If code turns out complex when its purpose is simple, look for a simpler implementation.

### 1.3 Concision and signal boosting

Use common idioms so readers recognise them instantly. When code *looks* idiomatic but differs subtly, boost the signal with a comment:

```go
// Good:
if err := doSomething(); err == nil { // if NO error
    // ...
}
```

### 1.4 Maintainability

Don't hide critical details in a single character or a helper that is easy to overlook:

```go
// Bad: = vs := silently changes meaning.
if match, err = store.Match(ctx, id); err != nil {

// Good:
m, err := store.Match(ctx, id)
if err != nil {
    return fmt.Errorf("load match %v: %w", id, err)
}
match = m
```

- Split dense boolean expressions into named parts.
- Use the same parameter and receiver names for the same concept everywhere, so that names are predictable.
- Minimise dependencies, both explicit and implicit. Don't rely on undocumented behaviour.

---

## 2. Formatting

- All Go source **must** be `gofmt`-formatted. Generated code should be formatted too (`go/format.Source`).
- **No fixed line length.** If a line feels too long, refactor rather than wrap. Don't split a line just before an indentation change (function signature, `if`), and don't split long strings such as URLs.

---

## 3. Naming

### 3.1 General

- Use `MixedCaps` / `mixedCaps`. Never `snake_case` or `SCREAMING_CASE`, including for constants (`MaxMatchSize`, not `MAX_MATCH_SIZE`). Local variables count as unexported.
- Names must not feel repetitive at the call site, should take context into account, and must not repeat what is already clear.
- **Domain names must use the vocabulary in [`CONTEXT.md`](../CONTEXT.md)** and avoid the terms it lists under *Avoid*. For example, `Participant` rather than `Player`, and `Bout` rather than `Game` for one play session. *(Project rule, not from the Google guide.)*

### 3.2 Functions and methods

Leave out anything the call site already tells the reader:

| Don't repeat… | Bad | Good |
|---|---|---|
| the package name | `swiss.NewSwissPairer` | `swiss.NewPairer` |
| the receiver type | `(s *Stage) StageFormat()` | `(s *Stage) Format()` |
| parameter names | `OverrideFirstWithSecond(dst, src)` | `Override(dst, src)` |
| result names or types | `TransformToJSON(c) *jsonconfig.Config` | `Transform(c) *jsonconfig.Config` |
| input/output types, or whether they are pointers | `ParseBracketFromString(s string)` | `ParseBracket(s string)` |

Add detail only to disambiguate, e.g. `WriteTextTo` and `WriteBinaryTo`.

- Functions that **return** something get noun-like names and **no `Get` prefix**: `(t *Tournament) Capacity() int`.
- Functions that **do** something get verb-like names: `(m *Match) RecordBout(r BoutResult) error`.
- Functions that differ only by type put the type at the end: `ParseInt`, `ParseInt64`. If one version is clearly primary, it can drop the suffix.

### 3.3 Packages

- A package name says what the package provides. **Never** use `util`, `helper`, `common`, `misc` or similar on their own. Picture the call site: `swiss.Pair(...)`, not `util.Pair(...)`.
- Avoid package names that force import renaming or that shadow good local variable names (such as `url` or `match`).

### 3.4 Shadowing

- *Stomping* (reusing a variable with `:=` in the **same** scope) is fine when the old value is no longer needed, e.g. `ctx, cancel := context.WithTimeout(ctx, d)`.
- *Shadowing* (`:=` in a **new** scope) is a common source of bugs. To update an outer variable inside a block, use `=`:

```go
// Bad: ctx after the if is still the caller's ctx.
if shorten {
    ctx, cancel := context.WithTimeout(ctx, 3*time.Second)
    defer cancel()
}

// Good:
if shorten {
    var cancel context.CancelFunc
    ctx, cancel = context.WithTimeout(ctx, 3*time.Second)
    defer cancel()
}
```

- Don't give variables the names of standard packages (`url`, `time`, `errors`, …) outside very small scopes.

### 3.5 Test doubles and helper packages

- Put test doubles for package `foo` in package `footest`.
- If only one type needs a double, name it by the kind of double: `footest.Stub`, not `footest.StubService`.
- If one type needs several doubles, name them by behaviour: `AlwaysAccepts`, `AlwaysRejects`.
- If several types need doubles, name them by kind and type: `StubAllocator`, `StubNotifier`.
- In tests, prefix variables that hold doubles so they stand out from production values: `spyAllocator`, not `alloc`.

---

## 4. Packages and files

- Group code that clients use together, or whose *implementation* is tightly coupled, in one package. If a user must import both packages to use either, merge them.
- Give conceptually distinct things their own small package, but don't put a whole project in one package or split into many packages that can't stand alone.
- Size files so a maintainer can guess which file holds something and find it once there. Avoid files thousands of lines long and swarms of tiny files. There is no "one type per file" rule.
- Long package documentation may go in a `doc.go` that holds only the package comment and clause.
- If code is useful as both a library and a binary, keep the CLI thin and make it just another client of the library.

---

## 5. Imports

- Group imports as standard library first, then everything else, separated by a blank line (see Decisions: *import grouping*).
- Rename generated protobuf and gRPC imports with a descriptive name plus a `pb` or `grpc` suffix, e.g. `matchpb`, `matchgrpc`. Prefer whole words to cryptic abbreviations.

---

## 6. Errors

### 6.1 Handle deliberately

Every error must be handled on purpose: returned, annotated, logged, or explicitly and visibly ignored with a reason. Consider whether the current function is the best place to handle it. When orchestrating related operations where only the first error matters, use `errgroup`.

### 6.2 Give errors structure when callers need to tell them apart

- Use sentinel values (`var ErrRegistrationClosed = errors.New("registration closed")`) or custom error types.
- Check them with `errors.Is` or `errors.As`. **Never** match on `err.Error()` strings, in production code or in tests.
- Expose extra data that callers need as struct fields, as `os.PathError` does, rather than inside the message.

### 6.3 Annotating errors

- Add context the caller doesn't already have. Don't repeat what the wrapped error already says (e.g. `os` errors already include the path).
- Don't add annotations that say nothing new: `fmt.Errorf("failed: %v", err)` should just be `return err`.
- **`%w`** wraps: use it inside the application when callers may need `errors.Is` or `errors.As` on the underlying error, and when the wrapped error is a documented part of your API.
- **`%v`** annotates without wrapping: use it when you deliberately hide the cause, especially at **system boundaries** (HTTP API, Game Server protocol, database), where internal errors are translated into the API's own error space.
- **Put `%w` at the end** (`"load stage %d: %w"`) so the printed text reads newest to oldest.
- **Exception:** when wrapping a *sentinel that categorises* the failure, put it first so the category leads the message:
  `fmt.Errorf("%w: capacity %d reached", ErrTournamentFull, cap)`.

### 6.4 Logging errors

- If you return an error, **don't also log it**. Let the caller decide, which avoids duplicate log lines.
- Log messages must say clearly what went wrong and include what a maintainer needs to diagnose it.
- **Never log PII or secrets.** This includes Player Identities beyond what is needed, and per-match tokens (ADR-0002).
- Reserve the error level for **actionable** conditions, not for "more serious than a warning".
- Don't pay for expensive log arguments when that level is disabled. Guard them with a level check.

### 6.5 Initialization, panics and invariants

- Return startup errors (bad flags, bad config) up to `main`. `main` then prints an **actionable** message and exits non-zero, with no stack trace.
- Libraries return errors. They must not panic or exit on bad input or transient failures.
- Terminate the process only if an invariant is broken and internal state can't be recovered.
- Panic only for API misuse (as the standard library does), for code the compiler can't tell is unreachable, or inside `init` / `Must…` functions.
- Internal panic/recover (e.g. in a parser) is allowed only if the panic **never crosses the package boundary**: a deferred `recover` at the public entry point turns it into an error and re-panics on anything it didn't raise itself.
- **Don't recover panics to keep a server alive.** Recovering far from the panic spreads corrupted state. Surface panics through monitoring and fix them. (`net/http` recovering handler panics is a known historical mistake; don't copy it.)

---

## 7. Documentation

### 7.1 What to document

Every exported identifier must have a doc comment. Beyond that, **document what is non-obvious or error-prone, and say why it matters**. Don't list every parameter.

| Topic | Implied, don't restate | Must document |
|---|---|---|
| **Context** | Cancelling `ctx` interrupts the call and returns `ctx.Err()`. | A different error on cancellation, other ways to stop it (e.g. `Stop()`), or requirements on the context's deadline or values. |
| **Concurrency** | Read-only operations are safe for concurrent use; mutating ones are not. | When it's unclear whether an operation mutates (e.g. an LRU `Lookup`), when the type provides its own synchronisation (document it on the type), and any concurrency requirements on interfaces that callers implement. |
| **Cleanup** | | Every cleanup the caller is responsible for (`Close`, `Stop`, `cancel`), with an example if it's unclear how. |
| **Errors** | | Significant sentinels and error types that the function returns, including whether a type is returned as a pointer (e.g. "the error will be of type `*ValidationError`"). Put package-wide error conventions in the package comment. |

Don't design APIs that need unusual context guarantees. Documenting such a requirement is the fallback, not a pattern to follow.

### 7.2 Godoc formatting

- Separate paragraphs with a blank `//` line.
- Indent code or preformatted lists/tables by two extra spaces in comments.
- Prefer **runnable examples** (`func ExampleX()` in `_test.go`) to code in comments. They show up in godoc and run as tests.
- Preview docs locally (`pkgsite`) when you change public API documentation.

---

## 8. Declarations

- Use `:=` for a new variable with a non-zero value: `i := 42`, not `var i = 42`.
- Use `var x T` for a zero value that **is ready for use** (e.g. an unmarshal target). Don't write `Point{X: 0, Y: 0}` or `[]int(nil)`.
- Use composite literals when initial members are known.
- For a pointer to a zero value, `new(T)` or `&T{}` are both fine. `new` hints that a non-zero value would need a constructor.
- Keep locks and other no-copy fields as **value** fields (`mu sync.Mutex`). The type then needs pointer receivers and must be passed as a pointer. If a composite is returned or always addressed, declare it as a pointer from the start.
- Initialise a map before writing to it. Reading a nil map is fine.
- Add **size hints** (`make([]T, 0, n)`) only when the final size is known or profiling justifies it. Over-allocating wastes memory.
- **Specify channel direction** (`<-chan T`, `chan<- T`) wherever possible.

---

## 9. Function signatures

- Keep parameter lists short. Adjacent parameters of the same type are easy to swap by mistake.
- If a function is growing complex, consider splitting it into simpler functions that share an unexported implementation.
- For many inputs, use one of these:
  - **Option struct**, passed as the last argument. Prefer it when most callers set several options or several functions share the options. Export the struct only if the function using it is exported.
  - **Variadic functional options.** Prefer these when most callers set none, there are many rarely used options, or options take arguments or can fail. Options must take a value (`WithCheckIn(enabled bool)`, not `EnableCheckIn()`), be applied in order, and the last one wins.
- **Never put a `context.Context` in an option struct.** It is always the first parameter.

---

## 10. Interfaces

- **Don't create an interface before there is a real need.** A "service" or "repository" doesn't need a named interface by default. Start with the concrete type.
- **Reuse existing interfaces**, including generated client or server interfaces. Don't wrap generated clients in hand-written interfaces just to test them. Use a real transport instead (§11.6).
- **Consumers define interfaces**, with only the methods they use. Producers export an interface only when the interface *is the product* (a protocol like `io.Writer`), to stop identical interfaces spreading across many packages, or to break an import cycle. An import cycle usually means the packages are split wrongly, so fix that first.
- **Don't export interfaces or test doubles only for tests.** Design the API so it can be tested through the real implementation's public API.
- **Keep interfaces small.** Document every interface in proportion to how much it asks of the reader: a single-method interface on the type, a multi-method interface on each method. Document unexported interfaces too.
- **Accept interfaces, return concrete types.** Return an interface only to:
  - hide methods that would let callers break invariants,
  - return one of several concrete types chosen at runtime (factory, strategy),
  - or break an import cycle.
  Returning `error` is the normal case.

---

## 11. Tests

### 11.1 Failures belong in the `Test` function

- **Don't use assertion libraries** (e.g. testify `assert`/`require`) or hand-written assertion helpers. Compare with `==` or `github.com/google/go-cmp/cmp` and report with `t.Errorf` inside the test.
- Failure messages name the call, the result and the expectation: `t.Errorf("Standing(%v) = %d, want %d", p, got, want)`. For diffs, use `cmp.Diff(want, got)` and say which way the diff reads (`(-want +got)`).
- To share validation logic, use a table-driven test, or a function that **returns** an `error` or a `cmp.Option` and leaves the pass/fail decision to the test.

### 11.2 Test helpers

- Test helpers (setup and cleanup) call `t.Helper()` and **fail with `t.Fatalf`** and a descriptive message, rather than returning errors to the test. Name them `mustX`.
- Register cleanup with `t.Cleanup`.
- If a helper can't fail, it doesn't need a `t` parameter.

### 11.3 `t.Error` vs `t.Fatal`

- Prefer `t.Error` and **keep going**, so one run reports every failure.
- Use `t.Fatal` when setup fails or when continuing is pointless.
- In table tests without subtests, a failing entry uses `t.Error` plus `continue`. Inside `t.Run`, use `t.Fatal`, which ends only that subtest.
- **Never call `t.Fatal`/`t.FailNow` from a goroutine** the test started. Use `t.Error` and return.

### 11.4 Table-driven tests

- Use **field names** in test-case struct literals when the table is long, has adjacent fields of the same type, or leaves fields at their zero value.

### 11.5 Setup scope

- Scope setup to the tests that need it: call `mustLoadX(t)` in each test. Don't use `init()` or package-level loading, which makes `go test -run TestUnrelated` pay for it.
- For expensive setup that only some tests use and that needs no teardown, a `sync.Once` behind a `mustX(t)` helper is acceptable.
- Use a custom `TestMain` **only** when *every* test in the package needs expensive setup *with teardown*, such as a shared Postgres. Put the logic in `runMain(ctx, m) (int, error)` so that deferred calls run before `os.Exit`.
- Tests must be hermetic. Any test that changes shared state (e.g. database rows) must restore it.

### 11.6 Real transports

When testing integrations over HTTP or gRPC, use the **real client** against a test server or test double (e.g. `httptest.Server`). Don't use a hand-written fake client. Prefer testing libraries from the dependency's own authors where they exist.

### 11.7 Acceptance tests for extension points

If external code implements one of our interfaces (e.g. a Game Server integration), provide a `footest.ExerciseX(..., impl) error` validator. It returns structured errors and calls `t.Fatal` only if setup fails.

---

## 12. Strings

- Use `+` for a few pieces: `"tournament: " + id`.
- Use `fmt.Sprintf` when formatting. When writing to an `io.Writer`, use `fmt.Fprintf` directly.
- Use `strings.Builder` to build a string piecemeal in a loop.
- Use `text/template` or `html/template` for complex output.
- Use backtick raw strings for multi-line constants.

---

## 13. Global state

- Packages **must not** expose APIs that depend on mutable package-level state: top-level variables, global registries, callback lists, or lazily created singleton clients such as a global DB pool or Agones client.
- Create instances and **pass dependencies explicitly** through constructors, parameters or struct fields. `main` wires everything together.
- Package-level state is acceptable only if one of these holds:
  - it is logically constant,
  - the package's observable behaviour is stateless (e.g. an invisible cache),
  - it doesn't leak outside the process,
  - no predictable behaviour is expected of it (e.g. `math/rand`).

  In every case, independent callers and tests must not be able to affect each other through it.
- A convenience default instance, like `http.DefaultServeMux`, is allowed only as a thin proxy over an instance API. Only binaries may use it, never libraries. It must document its invariants and offer a way to reset it.
- Existing code that breaks this rule is **not** a precedent.

---

## 14. Local consistency

Where this document is silent, follow the style already used by nearby code (same file, then package, then directory). Examples are `%v` vs `%s` for errors, or channels vs mutexes.

Local consistency never justifies breaking a rule in this document. It never justifies long lines or assertion libraries either. If a change would make an existing deviation worse or spread it, fix the deviation first, in the same PR or one before it.
