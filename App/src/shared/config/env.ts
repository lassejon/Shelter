import { z } from 'zod';

const envSchema = z.object({
  VITE_API_BASE_URL: z.url(),
  VITE_GOOGLE_MAPS_API_KEY: z.string(),
  VITE_GOOGLE_MAP_ID: z.string(),
});

const parsed = envSchema.safeParse(import.meta.env);

if (!parsed.success) {
  const issues = z.treeifyError(parsed.error);
  console.error('Invalid environment configuration:', issues);
  throw new Error(
    'Invalid environment configuration. See console for details. Check your .env file.',
  );
}

export const env = {
  apiBaseUrl: parsed.data.VITE_API_BASE_URL,
  googleMapsApiKey: parsed.data.VITE_GOOGLE_MAPS_API_KEY,
  googleMapId: parsed.data.VITE_GOOGLE_MAP_ID,
};
