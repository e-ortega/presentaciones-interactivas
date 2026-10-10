import { expect, test } from 'vitest'
import { firstPosition, lastPosition, nextPosition, positionForKey, previousPosition } from './navigation'

// Three slides; the middle one has three build steps, as an animated slide will.
const deck = [{ stepCount: 1 }, { stepCount: 3 }, { stepCount: 1 }]

test('forward goes through every step of a slide before the next slide', () => {
  const visited = [firstPosition]
  for (let next = nextPosition(deck, firstPosition); next !== null; next = nextPosition(deck, next)) {
    visited.push(next)
  }

  expect(visited).toEqual([
    { slideIndex: 0, stepIndex: 0 },
    { slideIndex: 1, stepIndex: 0 },
    { slideIndex: 1, stepIndex: 1 },
    { slideIndex: 1, stepIndex: 2 },
    { slideIndex: 2, stepIndex: 0 },
  ])
})

test('backward lands on the last step of the previous slide', () => {
  expect(previousPosition(deck, { slideIndex: 2, stepIndex: 0 })).toEqual({ slideIndex: 1, stepIndex: 2 })
  expect(previousPosition(deck, { slideIndex: 1, stepIndex: 2 })).toEqual({ slideIndex: 1, stepIndex: 1 })
  expect(previousPosition(deck, { slideIndex: 1, stepIndex: 0 })).toEqual({ slideIndex: 0, stepIndex: 0 })
})

test('there is nothing before the start or after the end', () => {
  expect(previousPosition(deck, firstPosition)).toBeNull()
  expect(nextPosition(deck, lastPosition(deck))).toBeNull()
})

test('the last position is the last step of the last slide', () => {
  expect(lastPosition([{ stepCount: 1 }, { stepCount: 4 }])).toEqual({ slideIndex: 1, stepIndex: 3 })
})

test.each(['ArrowRight', 'ArrowDown', 'PageDown', ' '])('the key "%s" moves forward', (key) => {
  expect(positionForKey(key, deck, firstPosition)).toEqual({ slideIndex: 1, stepIndex: 0 })
})

test.each(['ArrowLeft', 'ArrowUp', 'PageUp'])('the key "%s" moves back', (key) => {
  expect(positionForKey(key, deck, { slideIndex: 1, stepIndex: 0 })).toEqual(firstPosition)
})

test('Home and End jump to the ends of the deck', () => {
  expect(positionForKey('Home', deck, { slideIndex: 1, stepIndex: 1 })).toEqual(firstPosition)
  expect(positionForKey('End', deck, firstPosition)).toEqual({ slideIndex: 2, stepIndex: 0 })
})

test('at either end a navigation key stays in place instead of leaving the deck', () => {
  expect(positionForKey('ArrowLeft', deck, firstPosition)).toEqual(firstPosition)
  expect(positionForKey('ArrowRight', deck, lastPosition(deck))).toEqual(lastPosition(deck))
})

test('other keys are not navigation', () => {
  expect(positionForKey('a', deck, firstPosition)).toBeNull()
  expect(positionForKey('Enter', deck, firstPosition)).toBeNull()
})
