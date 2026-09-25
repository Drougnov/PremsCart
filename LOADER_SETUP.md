# Animated loader

The loader is already integrated in this updated project. You do not need to paste extra code.

Files:
- `frontend/public/loader.css`: animation, colors, sizes, reduced-motion rules.
- `frontend/public/boot-loader.js`: initial loading timeout and Retry button.
- `frontend/index.html`: loader displayed before React starts.
- `frontend/src/Loader.tsx`: reusable React loader and button spinner.
- `frontend/src/App.tsx`: account-loading integration.
- `frontend/src/Cart.tsx` and `ListingForm.tsx`: action spinners.

The loader uses your existing `/brand/premscart-logo.jpeg`. Edit the default label in Loader.tsx and the corresponding initial label in index.html to change its text. Colors use the `--pc-loader-*` CSS variables.

To use it for another asynchronous page:

```tsx
import Loader from './Loader'

// Place after your hooks and before the page content.
if (loading) return <Loader label="Loading your listings…" onRetry={reload} />
```

Use an actual loading state. Set it false when the request finishes and render an error with retry when it fails. Do not add a timer to delay a successful page just to display the animation. Existing skeletons remain for smaller content sections.

The initial and React loaders show slow-loading help after 12 seconds. Motion stops when the user's system requests reduced motion. A JavaScript-disabled browser gets an explanatory message instead of an endless loader.
