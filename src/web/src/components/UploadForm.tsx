import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState, type SubmitEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { ApiError } from '../api/http'
import { uploadPresentation } from '../api/presentations'
import buttonStyles from './Button.module.css'
import styles from './UploadForm.module.css'

/** Picks a PPTX and uploads it. The list refreshes to show the new import. */
export function UploadForm() {
  const { t, i18n } = useTranslation()
  const queryClient = useQueryClient()
  const [file, setFile] = useState<File | null>(null)
  const upload = useMutation({ mutationFn: uploadPresentation })

  function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    if (file === null) {
      return
    }

    const form = event.currentTarget
    upload.mutate(file, {
      onSuccess: () => {
        form.reset() // clears the file input
        setFile(null)
        // The list is out of date now: ask for it again.
        void queryClient.invalidateQueries({ queryKey: ['presentations'] })
      },
    })
  }

  /** The message for a rejected upload: one per server code, with its limit when it has one. */
  function errorMessage(error: Error): string {
    if (error instanceof ApiError) {
      const key = `upload.error.${error.code}`
      // A code this version of the interface does not know yet gets the generic message.
      if (error.code !== undefined && i18n.exists(key)) {
        return t(key, { limit: error.limit })
      }
      if (error.status === 413) {
        return t('upload.error.tooLargeForServer')
      }
    }
    return t('upload.error.generic')
  }

  return (
    <form className={styles.form} onSubmit={handleSubmit}>
      <label className={styles.label} htmlFor="pptx-file">
        {t('upload.label')}
      </label>
      <input
        id="pptx-file"
        className={styles.file}
        type="file"
        accept=".pptx"
        onChange={(event) => setFile(event.target.files?.[0] ?? null)}
      />

      <button className={buttonStyles.button} type="submit" disabled={file === null || upload.isPending}>
        {upload.isPending ? t('upload.submitting') : t('upload.submit')}
      </button>

      {upload.isError && (
        <p className={styles.error} role="alert">
          {errorMessage(upload.error)}
        </p>
      )}
    </form>
  )
}
