import { useTranslation } from 'react-i18next'
import type { SlideDetail } from '../api/types'
import styles from './ScriptPanel.module.css'

/** How the script column behaves (D-06). */
export type ScriptMode = 'teleprompter' | 'static'

type ScriptPanelProps = {
  slides: SlideDetail[]
  currentSlideIndex: number
  mode: ScriptMode
}

/**
 * The teacher's speaker notes, as a script. Notes are always shown as plain text:
 * they come from an uploaded file and are never treated as HTML.
 */
export function ScriptPanel({ slides, currentSlideIndex, mode }: ScriptPanelProps) {
  const { t } = useTranslation()

  if (mode === 'static') {
    // Every note of the deck in one list that stays where the teacher left it.
    return (
      <aside className={styles.panel} aria-label={t('presenter.script.label')}>
        <ol className={styles.allNotes}>
          {slides.map((slide) => (
            <li
              key={slide.index}
              className={styles.staticNote}
              aria-current={slide.index === currentSlideIndex ? 'true' : undefined}
            >
              <span className={styles.slideNumber}>{slide.index + 1}</span>
              <Notes text={slide.speakerNotes} />
            </li>
          ))}
        </ol>
      </aside>
    )
  }

  // Teleprompter: follows the slide. This slide's notes, then the next one's, dimmed.
  const nextSlide = slides.at(currentSlideIndex + 1)
  return (
    <aside className={styles.panel} aria-label={t('presenter.script.label')}>
      <div className={styles.current}>
        <Notes text={slides[currentSlideIndex].speakerNotes} />
      </div>
      {nextSlide && (
        <div className={styles.upcoming}>
          <p className={styles.upcomingLabel}>{t('presenter.script.next')}</p>
          <Notes text={nextSlide.speakerNotes} />
        </div>
      )}
    </aside>
  )
}

function Notes({ text }: { text: string }) {
  const { t } = useTranslation()

  if (text.trim() === '') {
    return <p className={styles.empty}>{t('presenter.script.empty')}</p>
  }
  return <p className={styles.notes}>{text}</p>
}
