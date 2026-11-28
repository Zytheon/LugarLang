# React + TypeScript + Vite

This template provides a minimal setup to get React working in Vite with HMR and some ESLint rules.

Currently, two official plugins are available:

- [@vitejs/plugin-react](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react) uses [Oxc](https://oxc.rs)
- [@vitejs/plugin-react-swc](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react-swc) uses [SWC](https://swc.rs/)

## React Compiler

The React Compiler is not enabled on this template because of its impact on dev & build performances. To add it, see [this documentation](https://react.dev/learn/react-compiler/installation).

## Expanding the ESLint configuration

If you are developing a production application, we recommend updating the configuration to enable type-aware lint rules:

```js
export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      // Other configs...

      // Remove tseslint.configs.recommended and replace with this
      tseslint.configs.recommendedTypeChecked,
      // Alternatively, use this for stricter rules
      tseslint.configs.strictTypeChecked,
      // Optionally, add this for stylistic rules
      tseslint.configs.stylisticTypeChecked,

      // Other configs...
    ],
    languageOptions: {
      parserOptions: {
        project: ['./tsconfig.node.json', './tsconfig.app.json'],
        tsconfigRootDir: import.meta.dirname,
      },
      // other options...
    },
  },
])

```

You can also install [eslint-plugin-react-x](https://npmx.dev/package/eslint-plugin-react-x) and [eslint-plugin-react-dom](https://npmx.dev/package/eslint-plugin-react-dom) for React-specific lint rules:

```js
// eslint.config.js
import reactX from 'eslint-plugin-react-x'
import reactDom from 'eslint-plugin-react-dom'

export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      // Other configs...
      // Enable lint rules for React
      reactX.configs['recommended-typescript'],
      // Enable lint rules for React DOM
      reactDom.configs.recommended,
    ],
    languageOptions: {
      parserOptions: {
        project: ['./tsconfig.node.json', './tsconfig.app.json'],
        tsconfigRootDir: import.meta.dirname,
      },
      // other options...
    },
  },
])

```

---

# LugarLang Web

LugarLang Web is the React and TypeScript web application for discovering places and exploring public transportation routes.

## Technology Stack

- React
- TypeScript
- Vite
- Leaflet and React Leaflet
- ESLint
- Playwright
- GitHub Actions
- Cloudflare Pages

## Local Development

Run `npm ci` to install dependencies and `npm run dev` to start the development server.

## Quality Checks

- `npm run lint` — ESLint checks.
- `npm run build` — TypeScript compilation and production build.
- `npm run test:e2e -- --project=chromium` — Playwright browser tests.

## Continuous Integration

GitHub Actions runs lint, build, and Playwright tests. The workflow is `.github/workflows/web-ci.yml`. The required status check is `Web Build and Browser Tests`.

## Branch and Pull Request Workflow

1. Create a feature branch.
2. Implement and test changes.
3. Push the branch to GitHub.
4. Open a pull request targeting `main`.
5. Wait for required CI checks to pass.
6. Merge the pull request.

The production branch is protected by the `Protect Production Branch` ruleset.

## Deployment

Cloudflare Pages automatically deploys changes pushed to the production branch.

- Repository: `Zytheon/LugarLang`
- Root directory: `LugarLang.Web`
- Build command: `npm run build`
- Output directory: `dist`
- Production branch: `main`

Production URL: https://lugarlang.pages.dev
