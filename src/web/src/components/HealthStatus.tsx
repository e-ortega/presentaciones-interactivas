import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { fetchHealth } from '../api/health'
import styles from './HealthStatus.module.css'

/** Shows whether the server is reachable. */
export function HealthStatus() {
  const { t } = useTranslation()
  const health = useQuery({ queryKey: ['health'], queryFn: fetchHealth })

  let message = t('health.up')
  if (health.isPending) {
    message = t('health.checking')
  } else if (health.isError) {
    message = t('health.down')
  }

  return (
    <p className={styles.status} role="status">
      {message}
    </p>
  )
}
