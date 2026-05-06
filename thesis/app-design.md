# App Design

The frontend lives at `/App` and is a single-page React application that consumes the Minimal API documented in `api-design.md`. This document records the architectural choices behind the rebuild and the bootstrap procedure that produces a working development environment.

The companion file `App/CLAUDE.md` is the source of truth for day-to-day rules (folder layout, contract deltas, phased migration plan); this document explains *why* those rules look the way they do.

---

## Technology choice: Vite + React 19 + TypeScript

The frontend is built on **Vite 7**, **React 19**, and **TypeScript 5.9** in strict mode. Routing is handled by **React Router 7** (`createBrowserRouter`), server state by **TanStack Query 5**, client state by **Zustand 5**, forms by **React Hook Form 7** with **Zod 4** for schemas, HTTP by **Axios**, styling by **Tailwind 4** via the official Vite plugin, and toasts by **sonner**.

### What was evaluated

| Option | Description |
|---|---|
| **Vite + React** | A pure SPA build tool. Native ES modules in development, Rollup in production, zero ceremony beyond a `vite.config.ts`. The default for greenfield React projects since CRA was officially deprecated. |
| **Next.js (App Router)** | A full framework with server-side rendering, file-based routing, and a server-component model. The default if any of the workload is server-rendered. |
| **Remix** | A framework optimised for nested routing and progressive enhancement; conceptually similar to Next.js but with a stronger emphasis on web platform primitives. |
| **Create React App** | The historical default. Officially deprecated in 2025; not a serious option for new work. |

### Why Vite + React

The project is a thin SPA consuming a JSON+JWT API. Server-side rendering would buy nothing — the entire application surface is interactive (a map-driven shelter browser, owner forms, booking flows), and there is no anonymous-friendly content that would benefit from being rendered server-side for SEO or first-paint. Adopting Next.js or Remix to render an authenticated SPA would be paying for capabilities that are never exercised.

Vite's value proposition is **build-tool minimalism**: a `vite.config.ts` of fewer than 20 lines configures the entire dev server, the production build, and the path-aliasing scheme. Hot module replacement is sub-100ms because the dev server serves modules natively rather than bundling. There is no implicit framework runtime to learn around — `defineConfig({ plugins: [react(), tailwindcss()] })` is the entirety of the build pipeline.

**React 19** is what the original Shelter prototype used and what the rebuild keeps. The interesting new capabilities introduced in 18 and 19 — concurrent rendering, transitions, the `use` hook, the `<form action>` integration — are largely orthogonal to this project's requirements but available if needed.

**TypeScript 5.9 in strict mode** is non-negotiable: the API surface is large enough that hand-typed DTOs would drift, and `openapi-typescript` (covered below) generates the request/response types directly from the API's OpenAPI document. Without strict mode the generator's nullability annotations would be silently weakened.

---

## Project structure: Feature-Sliced Design

The folder layout follows **Feature-Sliced Design** (FSD), the same pattern used by the original Shelter prototype but stated explicitly here so future work has a name to look up:

```
App/
  src/
    app/                         router, top-level providers, error boundary
      providers/
    features/                    one folder per feature
      <feature>/
        api/                     feature-scoped HTTP calls, returning parsed DTOs
        models/                  types, enums, Zod schemas
        hooks/                   useFoo, useCreateFoo (TanStack Query wrappers)
        stores/                  Zustand stores scoped to this feature
        components/              feature-specific UI
    shared/                      cross-feature concerns
      api/
        client.ts                axios instance + interceptors
        types/                   generated OpenAPI types (do not edit)
      ui/                        hand-rolled primitives (Button, Input, Field, ...)
      components/                cross-feature components (Header, ProtectedRoute)
      hooks/                     generic hooks (useDebounce, ...)
      stores/                    only for truly app-wide state
      config/
        env.ts                   Zod-parsed env at boot
      utils/
    pages/                       route shells; thin, compose feature components
```

The architecture has three rules that, together, do most of the work.

### 1. No cross-feature imports

A file inside `features/shelters/` may import from `shared/` and from its own subtree. It may **not** import from `features/auth/`, `features/bookings/`, or any other feature. If two features need the same code, that code is promoted to `shared/`. If it's a typed contract, the destination is `shared/api/types/`; if it's a UI primitive, `shared/ui/`; if it's a generic hook, `shared/hooks/`.

This is a structural constraint, not a stylistic one. The moment one feature imports from another, the dependency graph between features stops being a tree and starts being a graph, and "delete a feature" stops being a `rm -rf` operation. The original Shelter prototype enforced this by convention only and accumulated a small number of cross-feature imports over time; the rebuild treats violations as bugs.

### 2. Pages are thin

A file in `pages/` is a route shell. It reads route parameters, composes feature components, and gets out of the way. Business logic — data fetching, mutations, form state, derived display values — belongs in the feature that owns the data. A `ShelterDetailsPage` does not know how to load a shelter; it renders the components that do.

This is the frontend mirror of the API's "endpoint translates HTTP, handler does the work" rule. The page is the equivalent of the endpoint: it deals in URLs and route parameters and composes the actual functionality from elsewhere.

### 3. Promote on actual reuse, not in anticipation

The same rule applied on the API side: a DTO or component lives in its feature folder until a *second* feature genuinely needs it, at which point it moves to `shared/`. Pre-creating `shared/components/` modules in anticipation of reuse is the front-end version of pre-creating `Shared/` DTOs — the abstraction calcifies before any caller has shaped it, and the second use case ends up bending around the first.

---

## State boundaries

The hardest part of frontend architecture in 2026 is not picking *a* state library — it is deciding which kind of state belongs to which library. The project takes a strict line:

| State | Lives in | Why |
|---|---|---|
| Server data (shelters, bookings, reviews) | TanStack Query | caching, invalidation, retries, request deduplication, background refetching are all free |
| Authentication (token, userId, roles, displayName) | Zustand store, persisted to `localStorage` (key: `auth-storage`) | survives reload, single source of truth, simple subscription model |
| Map filters (search text, minRating, capacity, dates) | Zustand `map-filter.store.ts` (feature-scoped) | filters drive bbox queries, persist across navigation between map and detail pages |
| Form state | React Hook Form | local to the form, never globalised, validated by Zod |
| UI state (modal open, dropdown open, hover) | local `useState` | don't promote unless explicitly shared |

### Why TanStack Query for server data

The naive instinct is to put server data in Zustand or Redux and dispatch fetches as actions. This conflates two different concerns: the *data* (which is owned by the server) and the *cache* (which is owned by the client). TanStack Query provides a cache abstraction with the right invariants:

- A query key uniquely identifies a piece of server data.
- Stale data is shown immediately on remount; a fresh fetch happens in the background.
- Mutations can invalidate query keys, triggering a refetch the next time anything subscribes.
- Multiple components subscribing to the same key share one in-flight request.

Implementing the same set of behaviours by hand on top of Zustand is a six-month project that produces a worse version of TanStack Query. The default options are tuned for this codebase's read profile: `staleTime: 5m`, `gcTime: 10m`, `retry: 1`, `refetchOnWindowFocus: false`.

### Why Zustand (and not Redux Toolkit)

Auth state and filter state are both narrow stores with no time-travel debugging requirement, no middleware ecosystem expectations, and no cross-cutting selector composition needs. Redux Toolkit adds a slice abstraction, action creators, immer, and a developer-tools integration — value that lands when the project has a dozen interconnected slices but is overhead when there are two. Zustand's `create((set) => ({...}))` API is the smallest possible expression of "a global value with a setter". Persistence to `localStorage` is one middleware import.

The original Shelter prototype made one mistake worth flagging: there were two copies of the auth store, one in `shared/stores/` and one in `features/auth/stores/`, and the components imported from both. Any update path that touched only one copy silently went out of sync. The rebuild has **one canonical auth store**, in `features/auth/stores/auth.store.ts`. There is no copy in `shared/stores/`.

### Why React Hook Form for forms

`useState`-driven forms are the default React pattern but scale badly. Each input is its own piece of state, validation is hand-rolled in submit handlers, and the rerender story is "every keystroke triggers a top-level rerender of the form". RHF inverts this: form state is held in a ref, components subscribe via `register`, and only the field whose value changed rerenders. The form payload is read out at submit time as a single object.

Combined with Zod (next section), an RHF form is roughly: declare a schema, `useForm` with the schema as resolver, render fields wired by `register('fieldName')`, submit. The original Shelter prototype's `LoginDropdown` is a representative example of the alternative: 70 lines of `useState` plus a hand-written `emailRegex` validation block plus an `if (!email || !password)` guard plus a try/catch with branching error message extraction. The same form in RHF + Zod is roughly 25 lines and the validation is part of a schema that is also reusable for unit tests and OpenAPI request typing.

---

## Forms: RHF + Zod everywhere

Every form uses RHF + Zod. The Zod schema is the single source of truth, the TS type is derived via `z.infer`, and the resolver wires Zod into RHF. There is **no hand-rolled validation** in components: no `if (!email)` checks, no inline regex, no `useState<string | null>(null)` for error messages.

```ts
// features/auth/models/login.schema.ts
import { z } from 'zod';

export const loginSchema = z.object({
  email: z.string().email(),
  password: z.string().min(1, 'Password is required'),
});

export type LoginInput = z.infer<typeof loginSchema>;
```

```tsx
// features/auth/components/LoginForm.tsx
const form = useForm<LoginInput>({
  resolver: zodResolver(loginSchema),
});
```

The same pattern applies at the application boundary. `shared/config/env.ts` parses `import.meta.env` through a Zod schema at module load time and throws if a required environment variable is missing or malformed:

```ts
const envSchema = z.object({
  VITE_API_BASE_URL: z.url(),
  VITE_GOOGLE_MAPS_API_KEY: z.string(),
  VITE_GOOGLE_MAP_ID: z.string(),
});

const parsed = envSchema.safeParse(import.meta.env);
if (!parsed.success) throw new Error('Invalid environment configuration.');
```

A misconfigured deployment fails at boot with a clear error rather than producing strange runtime behaviour when an unset variable is silently `undefined`.

---

## Auth model

The API uses ASP.NET Core Identity backed by JWT bearer tokens. `POST /api/auth/login` returns an `AuthResponse` containing `userId`, `email`, `firstName`, `lastName`, `roles`, `accessToken`, and `expiresAtUtc`. The frontend persists this payload to `localStorage` via the Zustand `persist` middleware and attaches the token to every outgoing request via an axios interceptor.

### Roles drive route protection

Routes are guarded by two components in `shared/components/`:

- `<ProtectedRoute>` redirects to `/` if `isAuthenticated` is false.
- `<RoleProtectedRoute role="ShelterOwner">` redirects to `/` if the role is missing.

`roles` is read directly from the auth store; the API has already decoded the JWT for us and returned the role list as part of `AuthResponse`. There is no client-side JWT decoding because there is no need to — the canonical role list is the one in the store, refreshed on every login or upgrade.

The original Shelter prototype carried a `jwt.ts` utility that decoded the token client-side to extract roles. This was correct for Identity Server-style flows where the API doesn't return roles separately, but for our API the decoded payload is redundant with `AuthResponse.roles`. The rebuild drops the client-side decoder.

### Token storage: `localStorage`

Tokens are stored in `localStorage`. This is a documented tradeoff: `localStorage` is reachable from any JavaScript on the page, which means an XSS vulnerability — anywhere — exfiltrates the token. The standard alternative is an HTTP-only cookie set by the API, which the browser sends automatically and is unreadable from JavaScript. That solution is significantly more involved to wire up:

- The API has to be reconfigured to set the cookie on login (`Set-Cookie: shelter_auth=...; HttpOnly; Secure; SameSite=Lax`).
- Antiforgery tokens have to be re-enabled (currently disabled per `api-design.md`'s rationale; cookie-based auth re-introduces CSRF risk).
- The frontend has to send credentials on every request (`axios.defaults.withCredentials = true`).
- A refresh-token rotation flow has to be designed (cookies typically have shorter expiry than JWTs).
- Browser support for `SameSite=None` has cross-domain caveats that affect local development.

For an MVP / thesis project deployed behind authenticated administrative access, `localStorage` is acceptable. The plan explicitly defers the cookie migration as a separate, larger project.

### `GET /api/auth/me` and boot-time session refresh

The original Shelter prototype called `GET /api/auth/me` on every app boot to validate the persisted token and refresh user info. Phase 1 of the rebuild deferred the endpoint — the API didn't expose it, the frontend trusted the persisted store, and the 401 response interceptor was the only invalidation path. Phase 2 reintroduces `/me`. The endpoint returns a fresh `AuthResponse` for the current user: it re-reads the user from the Identity store, fetches the current role list, and re-signs a JWT with those roles plus a new expiry.

Three reasons this is worth a network round-trip on every app mount, not just a localStorage read:

- **The token may be revoked or expired.** The 401 interceptor catches this on the first authenticated request, but that means the user briefly sees a logged-in UI before being kicked out. A boot-time `/me` call lets the store reconcile to the logged-out state immediately, so route guards and conditional UI render correctly on the first frame the user sees.
- **Roles in the JWT may be stale.** An admin can promote or demote a user server-side without revoking their token. The roles claim baked into the JWT does not change retroactively. Without `/me`, the user keeps the privileges (or restrictions) that were true at the moment the token was issued, even if the server's view has moved on. `/me` re-issues a token whose roles claim matches the *current* server state.
- **Profile data may have changed.** First name, last name — anything else the API surfaces in `AuthResponse` — gets refreshed.

The frontend wires this in `app/SessionRefresh.tsx`, a tiny component mounted once inside the QueryProvider whose only job is to call `useMe()`. The hook is `enabled: Boolean(token)` so a logged-out user never makes the call. On success, the auth store is overwritten with the fresh response (the `setAuth` reducer handles the entire `AuthResponse` shape, including the re-issued token). On failure, the existing 401 interceptor in `shared/api/client.ts` invokes `logout()`, which clears the persisted state.

The interaction with `upgrade-to-owner` is worth noting: that endpoint *also* returns a fresh `AuthResponse` with the new role and an extended JWT, so the UI doesn't need a second `/me` call after upgrading. `/me` is genuinely a boot-time concern.

---

## API client and OpenAPI types

Every HTTP call goes through a single axios instance defined in `shared/api/client.ts`:

```ts
export const apiClient = axios.create({
  baseURL: env.apiBaseUrl,
  timeout: 10000,
  headers: { 'Content-Type': 'application/json' },
});

apiClient.interceptors.request.use((config) => {
  const token = useAuthStore.getState().token;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) useAuthStore.getState().logout();
    return Promise.reject(error);
  },
);
```

The two interceptors handle the two cross-cutting concerns that every authenticated request needs: attach the bearer, handle session invalidation. Per-feature `api/*.api.ts` modules wrap the client with typed helpers; they do not configure the client further.

### Generated types: `openapi-typescript`

DTO types are not hand-written. The API exposes its OpenAPI document at `http://localhost:44330/openapi/v1.json` (per `api-design.md`, the project uses `Microsoft.AspNetCore.OpenApi`, not Swashbuckle, so the path is `/openapi/v1.json`, not `/swagger/v1/swagger.json`). A single npm script regenerates the type definitions:

```
"gen:api": "openapi-typescript http://localhost:44330/openapi/v1.json -o src/shared/api/types/paths.d.ts"
```

The output is `paths.d.ts`, a `paths` map keyed by URL with request/response shapes for every operation. Per-feature API modules pick the relevant subset:

```ts
import type { paths } from '@/shared/api/types/paths';
type ShelterDetail = paths['/api/shelters/{id}']['get']['responses']['200']['content']['application/json'];
```

This is committed to the repo. The contract between API and frontend is the OpenAPI document; the generated `paths.d.ts` is the materialised form of that contract. After any API change, regenerate and commit. Drift is caught at the consuming call site by the type system — no contract drift can survive a successful TypeScript build.

### Why not a generated client (e.g. `openapi-fetch`, `orval`)

Tools like `orval` or `openapi-fetch` go further: they generate not just types but ready-to-use functions and React Query hooks. The project deliberately stops at types because the friction of writing a four-line `getShelter(id)` function with the right Query Key conventions is lower than the friction of customising a generator's output to match the project's hook patterns. Types are the contract; the calling code is project-specific.

### Dev port and CORS

The Vite dev server runs on **port 3000** (`vite.config.ts: server.port: 3000`). This is non-default — Vite's default is 5173 — and is required because the API's `Cors:AllowedOrigins` policy explicitly lists `http://localhost:3000` and `https://localhost:3001` (per `appsettings.Development.json`). Running on the Vite default would fail the preflight `OPTIONS` request before any application code runs. If the port has to change, the API's CORS policy has to be updated in lockstep.

---

## Zod and OpenAPI: two type systems for two layers

Both Zod and `openapi-typescript` produce TypeScript types, which makes it tempting to think of them as alternatives. They are not — they sit at different layers and exist for different reasons. Conflating them produces either redundant validation or — more dangerously — gaps where the project *thinks* it's validating something it isn't. The split is worth stating explicitly.

### Zod is a runtime validator that also yields a compile-time type

A Zod schema is a JavaScript object describing a shape (`z.object({ email: z.email(), ... })`). Until you call `schema.parse(data)` or `schema.safeParse(data)`, no validation has happened — the schema is just a value sitting in memory. Calling `parse` walks the input at runtime, throws on mismatch, and returns a typed result on success.

Zod also gives you a static TypeScript type, free, via `z.infer<typeof schema>`. That type is a compile-time deduction from the schema definition; it is not itself a runtime check. The runtime guarantee comes from the `parse` call, the compile-time guarantee comes from `z.infer`. The schema being one source of truth for both is the design's appeal.

Zod is used in three places in this project, all of them at *boundaries*:

- **Form input** (`features/auth/models/login.schema.ts`, `register.schema.ts`). React Hook Form receives `zodResolver(loginSchema)` and runs the schema against form values on submit; field errors land in `formState.errors`. The form's TypeScript type is `z.infer<typeof loginSchema>`, so field names and types are checked at compile time too. Validation here happens on data the project does not control: the user's keystrokes.
- **Environment variables** (`shared/config/env.ts`). At module load, `envSchema.safeParse(import.meta.env)` runs once; a misconfigured deployment fails at boot rather than producing strange `undefined` behaviour later. The runtime check is the whole point — `import.meta.env` is whatever Vite injected, and the type system cannot see that.
- **Outbound clamping** (planned for Phase 5). The API rejects `MinRating` outside 1–5 with a 400; the frontend will validate and clamp before dispatch so the user sees a friendly inline error rather than a network failure.

### `openapi-typescript` produces compile-time types only

DTO types for API requests and responses are not hand-written. The script `npm run gen:api` runs `openapi-typescript http://localhost:44330/openapi/v1.json -o src/shared/api/types/paths.d.ts`, which reads the OpenAPI document and emits a `.d.ts` file containing a `paths` map and a `components.schemas` map. The output is a TypeScript declaration file: it compiles to *nothing*. There is no runtime artefact, no `.js` shipped to the browser, no validation function — only types.

Per-feature API modules pick the relevant subsets:

```ts
import type { components } from '@/shared/api/types/paths';
type AuthResponse = components['schemas']['AuthResponse'];
type LoginRequest = components['schemas']['LoginRequest'];
```

These types describe what the API *says* it accepts and returns. If the API ever lies about its response shape — `roles: null` instead of `roles: string[]` — TypeScript cannot catch it, because the type assertion happens at compile time and the wire data only exists at runtime. The project accepts this exposure: the OpenAPI document is generated by the API itself from the same C# types the handlers use, so a drift requires both ends of the contract to disagree about what they signed.

### The split, stated as a table

| | Source of truth | Runtime presence | Compile-time type | What it validates |
|---|---|---|---|---|
| `openapi-typescript` (`paths.d.ts`) | API's OpenAPI document | none — `.d.ts` only | `paths['/api/...']`, `components['schemas'][...]` | wire shape, at compile time only |
| Zod schema (`loginSchema`) | hand-written in `models/` | the schema object + `parse()` | `z.infer<typeof loginSchema>` | form / env / outbound input, at runtime |

Practically: `LoginRequest` (from OpenAPI) describes what the API accepts. `LoginInput` (from `z.infer<typeof loginSchema>`) describes what the form collects. They happen to match by hand today; if they ever diverge accidentally, the call site of `loginApi(values)` errors at compile time because the function signature expects the OpenAPI-derived `LoginRequest`.

### Why we don't generate Zod schemas from OpenAPI

A natural follow-up question: tools like `openapi-zod-client`, `kubb`, and `orval`'s Zod plugin can read an OpenAPI document and emit Zod schemas, giving the project runtime validation of wire shapes "for free". The project deliberately doesn't adopt them, for the same reason it stops at types-only generation for the request/response shapes (covered in "Why not a generated client" above): the friction of customising a generator's output to match the project's hook patterns is higher than the friction of writing a four-line API function and a small Zod schema by hand. A generator produces *all* the code or *none* — partial generation requires tooling discipline that doesn't pay back at this scale. Wire validation is a feature the project may add later (an axios response interceptor that runs a generated Zod schema against every response would be a defensible safety net), but it stays out of scope for the rebuild.

The shorter version: `openapi-typescript` *is* the DTO generator the question is asking about — it generates the wire-type DTOs from the OpenAPI document. It just generates types, not runtime parsers. Zod fills the runtime-parser role for inputs the project owns; for wire data, the project trusts the type system and the API contract.

---

## UI primitives policy

The project does **not** install a component library. There is no shadcn/ui, no MUI, no Chakra, no Radix Themes (the original Shelter prototype carried `@radix-ui/react-dropdown-menu` and friends — the rebuild drops them, with the option to reintroduce specific Radix primitives later if a real need appears).

Primitives are hand-rolled in `shared/ui/` as we need them. The Phase 1 floor is `Button`, `Input`, and `Field` (a label + control + error wrapper). Adding a new primitive (`Tabs`, `Dropdown`, `Modal`, `Card`) happens when the first slice that needs one arrives. If two slices end up reimplementing the same primitive, the second one promotes it from the slice into `shared/ui/`.

### Why no component library

A component library is a commitment to a *design system* shaped by the library's author. shadcn ships components built on Radix with Tailwind styling baked in; MUI ships its own design language; Chakra has its own props API. Adopting any of them locks the project into that vocabulary across every feature surface, and "unsticking" later is expensive — components proliferate, props become load-bearing, custom theming accumulates.

The hand-rolled approach trades upfront work for full ownership. Phase 1's `Button.tsx` is 50 lines including type definitions. The Tailwind class strings inside it are the design system; changing them is a one-file edit. The cost of adding a component is "write 50 lines"; the cost of evolving a component is "edit 50 lines". This is the same tradeoff as the API's vertical-slice architecture: prefer ad-hoc clarity over inherited abstraction.

The case where a library does pay for itself is **complex behavioural primitives** — date pickers, command palettes, accessible dropdowns with keyboard navigation, modals with focus trapping. These are weeks of work to get right. When the project needs one, the relevant Radix primitive (or `react-day-picker`, etc.) goes in as a single dependency for that one component, not as the foundation of the entire UI layer.

### Tailwind 4 via `@tailwindcss/vite`

Tailwind 4 introduced an official Vite plugin (`@tailwindcss/vite`) that supersedes the older PostCSS-based setup. The Vite plugin path requires:

- One entry in `vite.config.ts` (`tailwindcss()` in the plugins array).
- One `@import 'tailwindcss';` in `src/index.css`.

It does **not** require `postcss.config.js`, `tailwind.config.js`, or any explicit content-glob configuration — Tailwind 4 detects classes by scanning source files automatically. The original Shelter prototype was on the PostCSS path and carried both config files; the rebuild's setup is two lines shorter and does not need a separate PostCSS layer.

### Design tokens

Components reference **semantic tokens** (`primary-*`, `accent-*`) rather than raw palette names (`emerald-*`, `amber-*`) so the brand colour can shift in one place without rewriting every component. Tokens are declared in a Tailwind 4 `@theme` block at the top of `src/index.css`:

```css
@theme {
  --color-primary-50:  #ecfdf5;
  --color-primary-500: #10b981;
  --color-primary-600: #059669;
  --color-primary-700: #047857;
  /* …full 50–950 scale… */

  --color-accent-50:   #fffbeb;
  --color-accent-500:  #f59e0b;
  /* …full scale… */

  --color-page-bg:     #f8fafc;

  --font-sans:
    system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto,
    Oxygen, Ubuntu, Cantarell, sans-serif;
}
```

Tailwind 4's compiler turns each `--color-<name>-<shade>` variable into the matching utility (`bg-primary-600`, `text-primary-600`, `border-primary-600`, `ring-primary-500`, etc.), so the tokens are usable everywhere a Tailwind colour utility is. The `--color-page-bg` and `--font-sans` tokens are referenced from CSS directly (`body { background-color: var(--color-page-bg); font-family: var(--font-sans); }`).

Two tokens are defined; the rest is intentionally Tailwind's built-in palette:

- **`primary`** is the brand colour — used for CTAs, links, focus rings, the brand wordmark, the "active" thumbnail border, and the avatar background. Currently an emerald scale.
- **`accent`** is reserved for emphasis colour against the brand — review stars, notifications, "new" indicators. Currently an amber scale.
- **Slate** is used as-is for neutrals (page background, borders, body text, secondary text). It pairs well with the emerald primary and is the de-facto neutral scale across the codebase, so aliasing it as `--color-neutral-*` would add an indirection without a payoff.
- **Red** is used as-is for destructive / error UI (logout button text, validation error messages, error banners). Same reasoning: there is one and only one "danger" hue in the design, and Tailwind's red scale is what every developer reaches for.

The discipline: when a primitive or component needs the brand colour, write `bg-primary-600`, not `bg-emerald-600`. When it needs a neutral or an error colour, `bg-slate-50` and `text-red-600` are fine. The semantic alias only earns its keep where the colour might plausibly change — for the brand, it might; for neutrals and errors, it won't.

Adding a new semantic token is one line in `@theme`. If a colour starts repeating across components without a semantic name (e.g. a "muted" text colour used in five places), promote it to a token. The same rule as `shared/ui/`: don't pre-create tokens in anticipation; promote on actual reuse.

---

## What is deliberately deferred

| Area | Decision |
|---|---|
| Tests (Vitest / Playwright) | No test infrastructure at bootstrap. Vitest can be added later if a real bug reproduces. The Zod schemas and the map clustering hooks (Phase 4) are the only places where unit tests are likely to pay off. End-to-end tests are out of scope for the thesis. |
| `i18n` | Not in scope. All UI strings are English-only. Adding `react-intl` or `i18next` later is a mechanical refactor; doing it speculatively now adds friction without a use case. |
| HTTP-only cookie auth | Documented tradeoff. Requires API changes; deferred as a separate project. |
| Component library | Not adopted. May reintroduce specific Radix primitives if and when complex behavioural components need them. |
| Storybook / component catalog | Out of scope. The hand-rolled primitives are simple enough that visual review happens in the actual app. |
| State machines (XState) | Not adopted. Forms are RHF, server state is TanStack Query, navigation state is React Router. No flow has the complexity that would warrant a formal state machine. |

These deferrals are explicit so a future contributor knows the absence of a tool is a decision, not an oversight.

---

# Bootstrap procedure (human steps)

The architecture above is the destination. This section documents the concrete steps to bring up an empty `App/` folder to a working Phase 0 dev environment. The agent-driven path that produced the current state writes every config file directly; the path a human would take is shorter because Vite's `create` command does most of the work.

## 1. Scaffold with `npm create vite`

From the repository root:

```bash
cd App
npm create vite@latest . -- --template react-ts
```

Answer the prompts: project name `shelter-app` (or accept the default), framework `React`, variant `TypeScript`. The command writes `package.json`, `vite.config.ts`, `tsconfig.json`, `tsconfig.app.json`, `tsconfig.node.json`, `eslint.config.js`, `index.html`, `src/main.tsx`, `src/App.tsx`, `src/App.css`, `src/index.css`, `src/vite-env.d.ts`, `public/vite.svg`, and a few demo assets.

## 2. Strip the demo

The template ships with a counter-button demo. Remove it:

```bash
rm src/App.tsx src/App.css src/assets/react.svg public/vite.svg
```

`src/index.css` can be reduced to a Tailwind import (added in step 4).

## 3. Install runtime dependencies

```bash
npm install \
  react-router \
  @tanstack/react-query \
  zustand \
  axios \
  react-hook-form \
  zod \
  @hookform/resolvers \
  sonner
```

Use `react-router` (the v7 unified package), not `react-router-dom`. The latter is the v6-and-earlier name; v7 absorbed it.

## 4. Install Tailwind 4 (Vite plugin path)

```bash
npm install -D tailwindcss @tailwindcss/vite
```

In `vite.config.ts`, add the plugin:

```ts
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import path from 'path';

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: { alias: { '@': path.resolve(__dirname, './src') } },
  server: { port: 3000, strictPort: true },
});
```

Two non-default settings: the `@/*` alias for FSD imports (`@/features/auth/...`), and `port: 3000` to satisfy the API's CORS allow-list. `strictPort: true` causes the dev server to fail rather than silently increment to 3001 if the port is taken — avoids debugging a phantom CORS failure.

In `src/index.css`:

```css
@import 'tailwindcss';
html, body, #root { margin: 0; padding: 0; height: 100%; width: 100%; }
```

## 5. Install dev tooling

```bash
npm install -D \
  prettier \
  eslint-config-prettier \
  openapi-typescript \
  @types/node
```

The Vite template already wires ESLint with TypeScript support; `eslint-config-prettier` disables ESLint rules that would fight Prettier. `@types/node` is required by `vite.config.ts` (for `path` and `__dirname`).

## 6. Configure path aliases in `tsconfig.app.json`

Add to `compilerOptions`:

```json
"baseUrl": ".",
"paths": { "@/*": ["./src/*"] }
```

This mirrors the Vite alias so TypeScript and the bundler agree on what `@/features/...` resolves to.

## 7. Write env validation and `.env`

Create `src/shared/config/env.ts`:

```ts
import { z } from 'zod';

const envSchema = z.object({
  VITE_API_BASE_URL: z.url(),
  VITE_GOOGLE_MAPS_API_KEY: z.string(),
  VITE_GOOGLE_MAP_ID: z.string(),
});

const parsed = envSchema.safeParse(import.meta.env);
if (!parsed.success) {
  console.error('Invalid environment configuration:', z.treeifyError(parsed.error));
  throw new Error('Invalid environment configuration. See console for details.');
}

export const env = {
  apiBaseUrl: parsed.data.VITE_API_BASE_URL,
  googleMapsApiKey: parsed.data.VITE_GOOGLE_MAPS_API_KEY,
  googleMapId: parsed.data.VITE_GOOGLE_MAP_ID,
};
```

Create `.env.example` (committed) and `.env` (git-ignored):

```
VITE_API_BASE_URL=http://localhost:44330
VITE_GOOGLE_MAPS_API_KEY=
VITE_GOOGLE_MAP_ID=
```

## 8. Create the FSD folder skeleton

```bash
mkdir -p \
  src/app/providers \
  src/features/{auth,shelters,bookings,reviews,map,search}/{api,components,hooks,models} \
  src/features/auth/stores \
  src/shared/{api/types,components,config,hooks,stores,ui,utils} \
  src/pages
```

Empty folders are not committed to Git unless they contain a file; either drop a `.gitkeep` in each or wait for the first real file to land.

## 9. Add the `gen:api` script

In `package.json`:

```json
"scripts": {
  "dev": "vite",
  "build": "tsc -b && vite build",
  "preview": "vite preview",
  "lint": "eslint .",
  "format": "prettier --write .",
  "gen:api": "openapi-typescript http://localhost:44330/openapi/v1.json -o src/shared/api/types/paths.d.ts"
}
```

`gen:api` requires the API to be running locally. Run it whenever the API contract changes; commit the resulting `paths.d.ts`.

## 10. Verify

```bash
npm run dev      # serves http://localhost:3000 with the placeholder
npx tsc -b       # silent
npm run build    # produces dist/ cleanly
```

At this point Phase 0 is complete. Phase 1 layers on the cross-cutting infrastructure: `app/App.tsx`, `app/router.tsx`, `app/providers/QueryProvider.tsx`, `app/ErrorBoundary.tsx`, `shared/api/client.ts`, `features/auth/stores/auth.store.ts`, the route guards in `shared/components/`, and the UI floor in `shared/ui/`. Phase 2 onwards is feature work.

---

## Migration plan summary

The full migration plan from the original Shelter prototype to the rebuild lives in `App/CLAUDE.md`. In summary:

| Phase | Scope |
|---|---|
| 0 | Bootstrap (this document, sections above) |
| 1 | Cross-cutting infrastructure: axios client, query provider, route guards, router skeleton, error boundary, UI floor |
| 2 | Auth: store, login/register/upgrade/me APIs, login dropdown, register form |
| 3 | Shelter detail (read-only) — validates auth + a real fetch end-to-end |
| 4 | Map + bbox search — port the deck.gl + supercluster stack from as-is |
| 5 | Search UI rebuild (the original prototype shipped this as TODO skeletons) |
| 6 | Owner: create shelter (multipart upload, location picker, RHF + Zod) |
| 7 | Bookings: detail-page widget + settings list + owner per-shelter view |
| 8 | Reviews: paged list, write/edit/delete, picture grid |
| 9 | Polish: toasts everywhere, real account settings, upgrade-to-owner UX |

Each phase ends with a working, deployable application. The migration sections of `App/CLAUDE.md` are deleted once Phase 9 lands; the architecture sections (and this document) stay.
