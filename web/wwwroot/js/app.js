window.setCurrentVersion = (version) => {
    // Get current version from local storage & compare to version passed in
    var currentVersion = localStorage.getItem('version');

    if (currentVersion !== version) {
        localStorage.setItem('version', version);

        if (currentVersion !== null) {
            document.getElementById("refreshButton").click();

            // Clear cache & remove service worker if version has changed
            caches.keys().then(function (names) {
                for (let name of names)
                    caches.delete(name);
            });

            navigator.serviceWorker.getRegistrations().then(function (registrations) {
                for (let registration of registrations)
                    registration.unregister()
            })

            setTimeout(function () {
                document.getElementById("refresh1").classList.remove("hidden");
                
                setTimeout(function () {
                    document.getElementById("refresh2").classList.remove("hidden");

                    setTimeout(function () {
                        window.location.reload(true);
                    }, 500);
                }, 1000);
            }, 1000);
        }
    }
}

window.setupPhoneNumberWatcher = (id) => {
    var tele = document.getElementById(id);

    tele.addEventListener('keyup', function (e) {
        if (event.key != 'Backspace' && (tele.value.length === 1 || tele.value.length === 5 || tele.value.length === 9)) {
            tele.value += '-';
        }
    });
}

window.clickSuccessButton = () => {
    document.getElementById("successButton").click()
}

window.clickFailedButton = () => {
    document.getElementById("failedButton").click()
}

window.clickElementById = (id) => {
    document.getElementById(id).click();
}


window.initializeFlowbite = () => {
    // On page load or when changing themes, best to add inline in `head` to avoid FOUC
    if (localStorage.getItem('color-theme') === 'dark' || (!('color-theme' in localStorage) && window.matchMedia('(prefers-color-scheme: dark)').matches)) {
        document.documentElement.classList.add('dark');
        document.documentElement.style.setProperty("--highlight-color", "#011627")
    } else {
        document.documentElement.classList.remove('dark')
        document.documentElement.style.setProperty("--highlight-color", "#9BBCC6")
    }

    initFlowbite();

    var themeToggleBtn = document.getElementById('theme-toggle');

    themeToggleBtn.addEventListener('click', function () {
        window.themeToggle();
    });

    // Ensure initFlowbite() runs after all page URL changes
    window.onpopstate = function () {
        initFlowbite();
    };
}

window.themeToggle = () => {
    let color = "#9BBCC6"

    // if set via local storage previously
    if (localStorage.getItem('color-theme')) {
        if (localStorage.getItem('color-theme') === 'light') {
            document.documentElement.classList.add('dark');
            localStorage.setItem('color-theme', 'dark');
            color = "#011627"
        } else {
            document.documentElement.classList.remove('dark');
            localStorage.setItem('color-theme', 'light');
        }

        // if NOT set via local storage previously
    } else {
        if (document.documentElement.classList.contains('dark')) {
            document.documentElement.classList.remove('dark');
            localStorage.setItem('color-theme', 'light');
        } else {
            document.documentElement.classList.add('dark');
            localStorage.setItem('color-theme', 'dark');
            color = "#011627"
        }
    }

    document.documentElement.style.setProperty("--highlight-color", color)
}

window.updateCategory = (category) => {
    var mySelect = document.getElementById("category");

    for(var i, j = 0; i = mySelect.options[j]; j++) {
        if(i.value == category) {
            mySelect.selectedIndex = j;
            break;
        }
    }

    // Navigate to #contact
    document.getElementById("contact").scrollIntoView();
}
