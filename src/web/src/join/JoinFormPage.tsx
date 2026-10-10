import { useMutation, useQuery } from '@tanstack/react-query'
import { useState, type SubmitEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useParams } from 'react-router'
import { ApiError } from '../api/http'
import { getJoinInfo, joinSession } from '../api/join'
import buttonStyles from '../components/Button.module.css'
import { legal } from '../legal'
import styles from './Join.module.css'

/**
 * Second step for a student, and where the QR code leads: which class this is, the privacy
 * notice, and the form with name, email and consent (Ley 8968, D-29).
 */
export function JoinFormPage() {
  const { t, i18n } = useTranslation()
  const navigate = useNavigate()
  const { code = '' } = useParams()
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  // The law asks for express consent: the box starts unticked and nothing ticks it but the student.
  const [consent, setConsent] = useState(false)

  const info = useQuery({ queryKey: ['join', code], queryFn: () => getJoinInfo(code) })
  const join = useMutation({
    mutationFn: () => joinSession(code, { name, email, consent }),
    onSuccess: ({ sessionId }) => void navigate(`/session/${sessionId}`),
  })

  if (info.isPending) {
    return (
      <main className={styles.page}>
        <p role="status">{t('join.lookingUp')}</p>
      </main>
    )
  }

  if (info.isError) {
    const notFound = info.error instanceof ApiError && info.error.status === 404
    return (
      <main className={styles.page}>
        <p role="alert">{t(notFound ? 'join.codeNotFound' : 'join.lookupFailed')}</p>
        <Link className={styles.link} to="/">
          {t('join.tryAnotherCode')}
        </Link>
      </main>
    )
  }

  function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    join.mutate()
  }

  function errorMessage(error: Error): string {
    if (error instanceof ApiError) {
      const key = `join.error.${error.code}`
      if (error.code !== undefined && i18n.exists(key)) {
        return t(key)
      }
      if (error.status === 429) {
        return t('join.error.tooManyAttempts')
      }
    }
    return t('join.error.generic')
  }

  return (
    <main className={styles.page}>
      <p className={styles.kicker}>{t('join.joining')}</p>
      <h1 className={styles.title}>{info.data.title}</h1>

      <form className={styles.form} onSubmit={handleSubmit}>
        {/* The notice comes before the fields: the law asks to inform before asking for the data. */}
        <p className={styles.notice}>
          <strong>{t('join.notice.lead')}</strong> {t('join.notice.text', { contact: legal.contactEmail })}
        </p>

        <label className={styles.label} htmlFor="join-name">
          {t('join.nameLabel')}
        </label>
        <input
          id="join-name"
          className={styles.input}
          autoComplete="name"
          maxLength={100}
          required
          value={name}
          onChange={(event) => setName(event.target.value)}
        />

        <label className={styles.label} htmlFor="join-email">
          {t('join.emailLabel')}
        </label>
        <input
          id="join-email"
          className={styles.input}
          type="email"
          autoComplete="email"
          maxLength={254}
          required
          value={email}
          onChange={(event) => setEmail(event.target.value)}
        />

        <label className={styles.consent}>
          <input type="checkbox" checked={consent} onChange={(event) => setConsent(event.target.checked)} />
          <span>{t('join.consent', { responsible: legal.responsible })}</span>
        </label>

        <button className={buttonStyles.button} type="submit" disabled={!consent || join.isPending}>
          {join.isPending ? t('join.submitting') : t('join.submit')}
        </button>

        {join.isError && (
          <p className={styles.error} role="alert">
            {errorMessage(join.error)}
          </p>
        )}

        <p className={styles.version}>{t('join.notice.version', { version: info.data.noticeVersion })}</p>
      </form>
    </main>
  )
}
