<script lang="ts">
  interface Props {
    label?: string;
  }

  let { label = 'stub-component' }: Props = $props();
</script>

<div data-testid="static-stub">{label}</div>
