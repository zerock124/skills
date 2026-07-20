import { defineConfig } from 'orval';

// 由後端 Swagger 單向驅動前端 API 型別與呼叫函式。
// 執行 `npm run generate:api` 前，先把後端的 swagger.json 放到 src/api/。
// 產出的 src/api/services 與 src/api/models 皆為產生物，請勿手改。
export default defineConfig({
  __PROJECT_KEY__: {
    input: {
      target: './src/api/swagger.json',
    },
    output: {
      target: './src/api/services/',
      schemas: './src/api/models/',
      mode: 'tags',
      indexFiles: true,
      client: 'axios-functions',
      override: {
        mutator: {
          path: './src/api/orval-fetch.ts',
          name: 'orvalFetch',
        },
      },
    },
  },
});
