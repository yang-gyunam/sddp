import type { Message } from '../types';

/** Display only: ownership and editing still use message.sender. */
export function getMessageAuthor(message: Message): Message['sender'] {
  return message.author?.kind === 'memberAgent'
    ? { id: message.author.id, name: message.author.name, avatarUrl: null }
    : message.sender;
}
