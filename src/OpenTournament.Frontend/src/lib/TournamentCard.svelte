<script>
  let name = "My Fun Tournament"
  let image2 = "https://img.daisyui.com/images/stock/photo-1606107557195-0e29a4b5b4aa.webp"
  let image = "https://www.bing.com/th?pid=Sgg&qlt=100&u=https%3A%2F%2Fimages.start.gg%2Fimages%2Ftournament%2F690255%2Fimage-c88974fd1206043e5b476a82c9279ae0-optimized.jpg&ehk=yHadyBC87dymLq2G%2BjnAoXQef7%2BZlNEPVpEvFahclps%3D&w=280&h=280&r=0"
  let badges = [ "Mario Kart", "2v2", "1v1" ]
  let location = "Online"
  let indicator_msg = "Registration Closing Soon"
  let url = "/tournament"
</script>
<div class="card bg-base-100 w-96 shadow-xl">
  <div class="indicator">
    <a href="{url}">
    <figure>
        <span class="indicator-item indicator-center indicator-bottom badge">{indicator_msg}</span>
        <img src="{ image }" alt="{ name }" />
    </figure>
    </a>
  </div>
  <div class="card-body">
    <h2 class="card-title">
      <a href="{url}">{ name }</a>
    </h2>
    <p>{ location }</p>
    <div class="card-actions justify-end">
      {#each badges as badge }
      <div class="badge badge-outline">{badge}</div>
      {/each}
    </div>
  </div>
</div>
