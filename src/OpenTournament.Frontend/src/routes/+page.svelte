<script>
	export const prerender = true;
	export const ssr = true;

    import HomePage from '$lib/components/homepage/index.js';
    //import { siteName } from '$lib/server/global.js';
    let siteName = 'ggparty.online';
</script>
<svelte:head>
    <title>{siteName}</title>
    <meta name="description" content="Tournaments and Brackets for competition" />
</svelte:head>
<HomePage></HomePage>
