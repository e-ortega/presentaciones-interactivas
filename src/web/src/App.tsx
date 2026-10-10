import { useTranslation } from 'react-i18next'
import styles from './App.module.css'
import { HealthStatus } from './components/HealthStatus'

function App() {
  const { t } = useTranslation()

  return (
    <main className={styles.page}>
      <h1 className={styles.title}>{t('app.name')}</h1>
      <p className={styles.tagline}>{t('app.tagline')}</p>
      <HealthStatus />
    </main>
  )
}

export default App
