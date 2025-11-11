<fieldset class="fieldset w-xs bg-base-200 border border-base-300 p-4 rounded-box">
    
    <legend class="fieldset-legend">Event</legend>

    <label class="fieldset-label">Name</label>
    <input type="text" class="input" placeholder="My awesome page" />

    <label class="fieldset-label">Description</label>
    <textarea class="textarea" placeholder="Event description .."></textarea>
    
    <label class="fieldset-label">Slug</label>
    <input type="text" class="input" placeholder="my-awesome-page" />

    <label class="fieldset-label">Location</label>
    <input type="text" class="input" placeholder="Name" />
    
    <label class="fieldset-label" htmlFor="event-visibility">Visibility</label>
    <select class="select" id="event-visibility">
        <option disabled selected>Pick a browser</option>
        <option>Public</option>
        <option>Private</option>
    </select>
    <span class="fieldset-label">Optional</span>

    <label class="fieldset-label">Start Date</label>
    <input type="text" class="input" placeholder="Name" />
    <calendar-date class="cally bg-base-100 border border-base-300 shadow-lg rounded-box">
        <svg aria-label="Previous" class="size-4" slot="previous" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="M15.75 19.5 8.25 12l7.5-7.5"></path></svg>
        <svg aria-label="Next" class="size-4" slot="next" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="m8.25 4.5 7.5 7.5-7.5 7.5"></path></svg>
        <calendar-month></calendar-month>
    </calendar-date>

    <label class="fieldset-label">End Date</label>
    <button popovertarget="cally-popover1" class="input input-border" id="cally1" style="anchor-name:--cally1">
        Pick a date
    </button>
    <div popover id="cally-popover1" class="dropdown bg-base-100 rounded-box shadow-lg" style="position-anchor:--cally1">
        <calendar-date class="cally" onchange={document.getElementById('cally1').innerText = this.value}>
            <svg aria-label="Previous" class="size-4" slot="previous" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path d="M15.75 19.5 8.25 12l7.5-7.5"></path></svg>
            <svg aria-label="Next" class="size-4" slot="next" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path d="m8.25 4.5 7.5 7.5-7.5 7.5"></path></svg>
            <calendar-month></calendar-month>
        </calendar-date>
    </div>
    
</fieldset>