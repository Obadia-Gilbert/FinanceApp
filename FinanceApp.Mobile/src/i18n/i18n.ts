import AsyncStorage from '@react-native-async-storage/async-storage';
import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import en from './locales/en.json';
import es from './locales/es.json';
import sw from './locales/sw.json';

export const APP_LANGUAGE_KEY = 'app_language';

export type AppLanguage = 'en' | 'sw' | 'es';

/** Maps API / i18n language tags to a supported app language. */
export function normalizeAppLanguage(code: string | undefined | null): AppLanguage {
  if (!code) return 'en';
  const c = code.trim().toLowerInvariant();
  if (c.startsWith('sw')) return 'sw';
  if (c.startsWith('es')) return 'es';
  return 'en';
}

export async function getStoredLanguage(): Promise<AppLanguage> {
  try {
    const s = await AsyncStorage.getItem(APP_LANGUAGE_KEY);
    if (s === 'sw' || s === 'es') return s;
    return 'en';
  } catch {
    return 'en';
  }
}

async function setStoredLanguage(code: AppLanguage): Promise<void> {
  await AsyncStorage.setItem(APP_LANGUAGE_KEY, code);
}

void i18n.use(initReactI18next).init({
  resources: {
    en: { translation: en },
    sw: { translation: sw },
    es: { translation: es },
  },
  lng: 'en',
  fallbackLng: 'en',
  interpolation: { escapeValue: false },
});

/** Call once on app start to apply language from storage (after sync init with en). */
export async function hydrateLanguageFromStorage(): Promise<void> {
  const lng = await getStoredLanguage();
  if (lng !== i18n.language) await i18n.changeLanguage(lng);
}

export async function setAppLanguage(code: AppLanguage): Promise<void> {
  await setStoredLanguage(code);
  await i18n.changeLanguage(code);
}

export default i18n;
