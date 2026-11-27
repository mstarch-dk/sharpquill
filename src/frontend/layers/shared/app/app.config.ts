export default defineAppConfig({
  sharedLayer: {
    // True when the app's theme swaps the primary colour scale in dark mode (the website themes do), so shared
    // components must not add their own dark: primary variants
    darkModeSwapsPrimary: false
  }
})
