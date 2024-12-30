
<section class="relative min-h-screen pt-20">
    <div class="absolute inset-0 bg-[url('https://svelte0.dev/images/ui-placeholder.png')] bg-cover bg-center opacity-20"></div>
    <div class="relative mx-auto max-w-7xl px-4 py-20">
        <div class="flex flex-col items-center text-center" in:fade>
            <h1 class="mb-6 text-4xl font-bold leading-tight md:text-6xl lg:text-7xl">
                Power Up Your <span class="text-blue-500">Tournament</span>
                Experience
            </h1>
            <p class="mb-8 max-w-2xl text-lg text-gray-300">Host and compete in esports tournaments. Join the largest competitive gaming community and take your gaming to the next level.</p>
            <div class="flex flex-col gap-4 sm:flex-row">
                <div id="appleid-signin"></div>
                <script type="text/javascript" src="https://appleid.cdn-apple.com/appleauth/static/jsapi/appleid/1/en_US/appleid.auth.js"></script>
                <button class="rounded-full bg-blue-600 px-8 py-3 font-semibold hover:bg-blue-700" in:fly={{ y: 20, delay: 200 }}>Create Tournament</button>
                <button class="rounded-full border border-white/20 px-8 py-3 font-semibold hover:bg-white/10" in:fly={{ y: 20, delay: 400 }}>Browse Tournaments</button>
            </div>
        </div>
    </div>
</section>