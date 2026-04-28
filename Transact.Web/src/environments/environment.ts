// When served from the Angular dev server (ng serve on :4200), call the API
// directly on its dev port. When served from any other origin (e.g. nginx in
// Docker), call the same origin under /api — nginx will reverse-proxy it to
// the API container.
const isAngularDevServer =
  typeof window !== 'undefined' && window.location.port === '4200';

export const environment = {
  production: !isAngularDevServer,
  apiBaseUrl: isAngularDevServer ? 'http://localhost:5125' : '/api'
};
