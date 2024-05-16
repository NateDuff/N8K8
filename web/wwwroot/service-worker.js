// Service Worker script for PWA installation

// Define a unique cache name
var version = '0.0.1'
var cacheName = 'pwa-cache-v' + version;

// List of files to cache
var filesToCache = [
    '/favicon.svg',
    '/logo.svg'
];

var filesToExcludePattern = /^\/appsettings.*\.json$/;

// Install event
self.addEventListener('install', function(event) {
  console.log('Service Worker: Installing');

  // Perform installation tasks
  event.waitUntil(
    caches.open(cacheName)
      .then(function(cache) {
        console.log('Service Worker: Caching app shell');
        
        var filesToCacheFiltered = filesToCache.filter(function(file) {
            return !filesToExcludePattern.test(file);
        });

        return cache.addAll(filesToCacheFiltered);
      })
  );
});

// Activate event
self.addEventListener('activate', function(event) {
  console.log('Service Worker: Activating');

  // Remove old caches
  event.waitUntil(
    caches.keys().then(function(cacheNames) {
      return Promise.all(
        cacheNames.map(function(name) {
          if (name !== cacheName) {
            console.log('Service Worker: Removing old cache:', name);
            return caches.delete(name);
          }
        })
      );
    })
  );
});

// Fetch event
self.addEventListener('fetch', function(event) {
  //console.log('Service Worker: Fetching', event.request.url);

  // Serve from cache if available, otherwise fetch from network
  event.respondWith(
    caches.match(event.request).then(function(response) {
      return response || fetch(event.request);
    })
  );
});

// Add event listener for app install
self.addEventListener('beforeinstallprompt', function(event) {
  // Prevent Chrome 67 and earlier from automatically showing the prompt
  event.preventDefault();
  // Stash the event so it can be triggered later.
  deferredPrompt = event;
  // Update UI to notify the user they can add to home screen
  showInstallPromotion();
});