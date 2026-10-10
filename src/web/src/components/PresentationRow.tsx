import { useMutation } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router'
import { stepImageUrl } from '../api/presentations'
import { createSession } from '../api/sessions'
import type { PresentationSummary } from '../api/types'
import { firstPosition } from '../presenter/navigation'
import styles from './PresentationRow.module.css'

type PresentationRowProps = {
  presentation: PresentationSummary
}

const dateFormat = new Intl.DateTimeFormat('es-CR', { dateStyle: 'medium', timeStyle: 'short' })

/** One line of the presentation list: first slide, title, import status and what to do with it. */
export function PresentationRow({ presentation }: PresentationRowProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  // Starting a class opens a session, with its own join code, and goes to its lobby.
  const startClass = useMutation({
    mutationFn: () => createSession(presentation.id),
    onSuccess: (session) => void navigate(`/present/${session.id}`),
  })
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
        {startClass.isError && (
          <p className={styles.failure} role="alert">
            {t('presentations.startFailed')}
          </p>
        )}
      </div>

      {isReady && (
        <div className={styles.actions}>
          <button
            className={styles.start}
            type="button"
            disabled={startClass.isPending}
            onClick={() => startClass.mutate()}
            aria-label={t('presentations.startLabel', { title: presentation.title })}
          >
            {t('presentations.start')}
          </button>
          <Link
            className={styles.rehearse}
            to={`/admin/presentations/${presentation.id}/present`}
            aria-label={t('presentations.rehearseLabel', { title: presentation.title })}
          >
            {t('presentations.rehearse')}
          </Link>
        </div>
      )}
    </li>
  )
}
