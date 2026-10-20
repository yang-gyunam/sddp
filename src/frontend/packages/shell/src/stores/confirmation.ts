import { onDestroy } from 'svelte';
import {
  requestChoice,
  requestConfirmation,
  type ChoiceRequest,
  type ConfirmationOptions,
} from './confirmation.store';

/** Bind pending decisions to the component and the entity/project they were opened for. */
export function createConfirmation(getContext: () => string = () => '') {
  const lifetime = new AbortController();
  onDestroy(() => lifetime.abort());

  return {
    async confirmAction(
      message: string,
      options: Omit<ConfirmationOptions, 'signal'> = {}
    ): Promise<boolean> {
      const context = getContext();
      const result = await requestConfirmation(message, { ...options, signal: lifetime.signal });
      return !lifetime.signal.aborted && getContext() === context && result;
    },
    async chooseAction<T extends string>(
      request: Omit<ChoiceRequest<T>, 'signal'>
    ): Promise<T | null> {
      const context = getContext();
      const result = await requestChoice({ ...request, signal: lifetime.signal });
      return !lifetime.signal.aborted && getContext() === context ? result : null;
    },
  };
}
