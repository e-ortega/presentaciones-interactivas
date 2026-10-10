import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { ApiError } from '../api/http'
import { uploadPresentation } from '../api/presentations'
import buttonStyles from './Button.module.css'
import styles from './UploadForm.module.css'

const knownErrorCodes = ['NoFile', 'NotPptx', 'TooLarge', 'Unreadable', 'NoVisibleSlides', 'TooManySlides']

/** Picks a PPTX and uploads it. The list refreshes to show the new import. */
export function UploadForm() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [file, setFile] = useState<File | null>(null)
  const upload = useMutation({ mutationFn: uploadPresentation })

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
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
          {t(errorMessageKey(upload.error))}
        </p>
      )}
    </form>
  )
}

function errorMessageKey(error: Error): string {
  if (error instanceof ApiError && error.status === 413) {
    return 'upload.error.TooLarge'
  }
  if (error instanceof ApiError && error.code !== undefined && knownErrorCodes.includes(error.code)) {
    return `upload.error.${error.code}`
  }
  return 'upload.error.generic'
}
