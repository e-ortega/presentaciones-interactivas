import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { stepImageUrl } from '../api/presentations'
import type { PresentationSummary } from '../api/types'
import { firstPosition } from '../presenter/navigation'
import styles from './PresentationRow.module.css'

type PresentationRowProps = {
  presentation: PresentationSummary
}

const dateFormat = new Intl.DateTimeFormat('es-CR', { dateStyle: 'medium', timeStyle: 'short' })

/** One line of the presentation list: first slide, title, import status and the way to present it. */
export function PresentationRow({ presentation }: PresentationRowProps) {
  const { t } = useTranslation()
  const isReady = presentation.status === 'Ready'
  const statusText = isReady
    ? t('presentations.slides', { count: presentation.slideCount })
    : t(`presentations.status.${presentation.status}`)

  return (
    <li className={styles.row}>
      <div className={styles.thumbnail}>
        {isReady && <img src={stepImageUrl(presentation.id, firstPosition)} alt="" loading="lazy" />}
      </div>

      <div>
        <h3 className={styles.title}>{presentation.title}</h3>
        <p className={styles.detail}>
          {statusText} · {dateFormat.format(new Date(presentation.createdAt))}
        </p>
        {presentation.failure && <p className={styles.failure}>{t(`presentations.failure.${presentation.failure}`)}</p>}
      </div>

      {isReady && (
        <Link className={styles.present} to={`/admin/presentations/${presentation.id}/present`}>
          {t('presentations.present')}
        </Link>
      )}
    </li>
  )
}
