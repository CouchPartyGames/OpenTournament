<script>
import EventBanner from "$lib/event/EventBanner.svelte";
import { page } from '$app/stores';
</script>

<div>
    <img src="" />
</div>
<h1>Event {$page.params.slug}</h1>
<EventBanner></EventBanner>