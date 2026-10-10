import { useMutation } from '@tanstack/react-query'
import { useState, type SubmitEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { enterAccessKey } from '../api/access'
import { ApiError } from '../api/http'
import styles from './AccessKeyForm.module.css'
import buttonStyles from './Button.module.css'

type AccessKeyFormProps = {
  /** Called once the server has accepted the key. */
  onAccepted: () => void
}

/** Asks for the teacher access key (D-45). */
export function AccessKeyForm({ onAccepted }: AccessKeyFormProps) {
  const { t } = useTranslation()
  const [key, setKey] = useState('')
  const signIn = useMutation({ mutationFn: enterAccessKey, onSuccess: onAccepted })

  function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault() // the browser would reload the page
    signIn.mutate(key)
  }

  return (
    <form className={styles.form} onSubmit={handleSubmit}>
      <h2 className={styles.title}>{t('access.title')}</h2>

      <label className={styles.label} htmlFor="access-key">
        {t('access.keyLabel')}
      </label>
      <input
        id="access-key"
        className={styles.input}
        type="password"
        autoComplete="current-password"
        required
        value={key}
        onChange={(event) => setKey(event.target.value)}
      />

      <button className={buttonStyles.button}type="submit" disabled={signIn.isPending}>
        {signIn.isPending ? t('access.submitting') : t('access.submit')}
      </button>

      {signIn.isError && (
        <p className={styles.error} role="alert">
          {t(errorMessageKey(signIn.error))}
        </p>
      )}
    </form>
  )
}

function errorMessageKey(error: Error): string {
  if (error instanceof ApiError && error.status === 401) {
    return 'access.wrongKey'
  }
  if (error instanceof ApiError && error.status === 429) {
    return 'access.tooManyAttempts'
  }
  return 'access.failed'
}
