import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import es from './es.json'

// All interface texts live in resource files (D-44). Adding English later
// means adding en.json here.
void i18n.use(initReactI18next).init({
  resources: { es: { translation: es } },
  lng: 'es',
  fallbackLng: 'es',
  interpolation: { escapeValue: false }, // React already escapes text
})

export default i18n
