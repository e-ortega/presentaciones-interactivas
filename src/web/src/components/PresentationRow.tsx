import { useTranslation } from 'react-i18next'
import { firstSlideImageUrl } from '../api/presentations'
import type { PresentationSummary } from '../api/types'
import styles from './PresentationRow.module.css'

type PresentationRowProps = {
  presentation: PresentationSummary
}

const dateFormat = new Intl.DateTimeFormat('es-CR', { dateStyle: 'medium', timeStyle: 'short' })

/** One line of the presentation list: first slide, title and import status. */
export function PresentationRow({ presentation }: PresentationRowProps) {
  const { t } = useTranslation()
  const isReady = presentation.status === 'Ready'
  const statusText = isReady
    ? t('presentations.slides', { count: presentation.slideCount })
    : t(`presentations.status.${presentation.status}`)

  return (
    <li className={styles.row}>
      <div className={styles.thumbnail}>
        {isReady && <img src={firstSlideImageUrl(presentation.id)} alt="" loading="lazy" />}
      </div>

      <div>
        <h3 className={styles.title}>{presentation.title}</h3>
        <p className={styles.detail}>
          {statusText} · {dateFormat.format(new Date(presentation.createdAt))}
        </p>
        {presentation.failure && <p className={styles.failure}>{t(`presentations.failure.${presentation.failure}`)}</p>}
      </div>
    </li>
  )
}
