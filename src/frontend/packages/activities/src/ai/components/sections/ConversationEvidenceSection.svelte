<script lang="ts">
  import type { ConversationEvidenceContext } from '../../types/evidence.types';
  import { parseEvidenceReferences } from '../../utils/conversation-evidence';
  import ConversationEvidence from '../idioms/ConversationEvidence.svelte';

  let { references, compactUnresolved = false }: ConversationEvidenceContext = $props();
</script>

<ConversationEvidence
  items={parseEvidenceReferences(references)}
  {compactUnresolved}
  canLoad={false}
  visible={false}
  selected={null}
  source={null}
  loading={false}
  failed={false}
  onselect={() => {}}
  onclose={() => {}}
/>
