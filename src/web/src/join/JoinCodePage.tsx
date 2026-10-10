import { useState, type SubmitEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router'
import { looksLikeSessionCode, normalizeSessionCode } from '../api/join'
import buttonStyles from '../components/Button.module.css'
import styles from './Join.module.css'

/** The front door for students: type the code of the class. */
export function JoinCodePage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [code, setCode] = useState('')
  const [rejected, setRejected] = useState(false)

  function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!looksLikeSessionCode(code)) {
      setRejected(true)
      return
    }
    void navigate(`/s/${normalizeSessionCode(code)}`)
  }

  return (
    <main className={styles.page}>
      <h1 className={styles.title}>{t('join.title')}</h1>

      <form className={styles.form} onSubmit={handleSubmit}>
        <label className={styles.label} htmlFor="session-code">
          {t('join.codeLabel')}
        </label>
        <input
          id="session-code"
          className={styles.codeInput}
          inputMode="numeric"
          autoComplete="off"
          placeholder="0000-0000"
          value={code}
          onChange={(event) => {
            setCode(event.target.value)
            setRejected(false)
          }}
        />
        {rejected && (
          <p className={styles.error} role="alert">
            {t('join.codeFormat')}
          </p>
        )}
        <button className={buttonStyles.button} type="submit">
          {t('join.continue')}
        </button>
      </form>

      <p className={styles.aside}>
        <Link className={styles.link} to="/admin">
          {t('join.teacherLink')}
        </Link>
      </p>
    </main>
  )
}
