import js from '@eslint/js'
import tseslint from 'typescript-eslint'
import vue from 'eslint-plugin-vue'

export default tseslint.config(
  { ignores: ['.nuxt/**', '.output/**', 'node_modules/**', 'coverage/**'] },
  js.configs.recommended,
  ...tseslint.configs.recommended,
  ...vue.configs['flat/essential'],
  {
    rules: {
      // Nuxt auto-imports are unknown to ESLint; vue-tsc (npm run typecheck) already reports undefined names
      'no-undef': 'off',
      // Nuxt route files (index.vue, [slug].vue, error.vue) are single-word by convention
      'vue/multi-word-component-names': 'off'
    }
  },
  {
    files: ['**/*.vue'],
    languageOptions: { parserOptions: { parser: tseslint.parser } }
  }
)
