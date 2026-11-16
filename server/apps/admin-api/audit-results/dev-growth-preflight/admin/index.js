var __defProp = Object.defineProperty;
var __name = (target, value) => __defProp(target, "name", { value, configurable: true });

// ../../../../gacha-audit-20260907/server/node_modules/hono/dist/compose.js
var compose = /* @__PURE__ */ __name((middleware, onError, onNotFound) => {
  return (context, next) => {
    let index = -1;
    return dispatch(0);
    async function dispatch(i) {
      if (i <= index) {
        throw new Error("next() called multiple times");
      }
      index = i;
      let res;
      let isError = false;
      let handler;
      if (middleware[i]) {
        handler = middleware[i][0][0];
        context.req.routeIndex = i;
      } else {
        handler = i === middleware.length && next || void 0;
      }
      if (handler) {
        try {
          res = await handler(context, () => dispatch(i + 1));
        } catch (err) {
          if (err instanceof Error && onError) {
            context.error = err;
            res = await onError(err, context);
            isError = true;
          } else {
            throw err;
          }
        }
      } else {
        if (context.finalized === false && onNotFound) {
          res = await onNotFound(context);
        }
      }
      if (res && (context.finalized === false || isError)) {
        context.res = res;
      }
      return context;
    }
    __name(dispatch, "dispatch");
  };
}, "compose");

// ../../../../gacha-audit-20260907/server/node_modules/hono/dist/request/constants.js
var GET_MATCH_RESULT = /* @__PURE__ */ Symbol();

// ../../../../gacha-audit-20260907/server/node_modules/hono/dist/utils/buffer.js
var bufferToFormData = /* @__PURE__ */ __name((arrayBuffer, contentType) => {
  const response = new Response(arrayBuffer, {
    headers: {
      // Normalize the media type (case-insensitive) while keeping parameters like the boundary
      "Content-Type": contentType.replace(/^[^;]+/, (mediaType) => mediaType.toLowerCase())
    }
  });
  return response.formData();
}, "bufferToFormData");

// ../../../../gacha-audit-20260907/server/node_modules/hono/dist/utils/body.js
var isRawRequest = /* @__PURE__ */ __name((request) => "headers" in request, "isRawRequest");
var parseBody = /* @__PURE__ */ __name(async (request, options = /* @__PURE__ */ Object.create(null)) => {
  const { all = false, dot = false } = options;
  const headers = isRawRequest(request) ? request.headers : request.raw.headers;
  const contentType = headers.get("Content-Type");
  const mediaType = contentType?.split(";")[0].trim().toLowerCase();
  if (mediaType === "multipart/form-data" || mediaType === "application/x-www-form-urlencoded") {
    return parseFormData(request, { all, dot });
  }
  return {};
}, "parseBody");
async function parseFormData(request, options) {
  if (!isRawRequest(request) && request.bodyCache.formData) {
    return convertFormDataToBodyData(
      await request.bodyCache.formData,
      options
    );
  }
  const headers = isRawRequest(request) ? request.headers : request.raw.headers;
  const arrayBuffer = await request.arrayBuffer();
  const formDataPromise = bufferToFormData(arrayBuffer, headers.get("Content-Type") || "");
  if (!isRawRequest(request)) {
    request.bodyCache.formData = formDataPromise;
  }
  const formData = await formDataPromise;
  if (formData) {
    return convertFormDataToBodyData(formData, options);
  }
  return {};
}
__name(parseFormData, "parseFormData");
function convertFormDataToBodyData(formData, options) {
  const form = /* @__PURE__ */ Object.create(null);
  formData.forEach((value, key) => {
    const shouldParseAllValues = options.all || key.endsWith("[]");
    if (!shouldParseAllValues) {
      form[key] = value;
    } else {
      handleParsingAllValues(form, key, value);
    }
  });
  if (options.dot) {
    Object.entries(form).forEach(([key, value]) => {
      const shouldParseDotValues = key.includes(".");
      if (shouldParseDotValues) {
        handleParsingNestedValues(form, key, value);
        delete form[key];
      }
    });
  }
  return form;
}
__name(convertFormDataToBodyData, "convertFormDataToBodyData");
var handleParsingAllValues = /* @__PURE__ */ __name((form, key, value) => {
  if (form[key] !== void 0) {
    if (Array.isArray(form[key])) {
      ;
      form[key].push(value);
    } else {
      form[key] = [form[key], value];
    }
  } else {
    if (!key.endsWith("[]")) {
      form[key] = value;
    } else {
      form[key] = [value];
    }
  }
}, "handleParsingAllValues");
var handleParsingNestedValues = /* @__PURE__ */ __name((form, key, value) => {
  if (/(?:^|\.)__proto__\./.test(key)) {
    return;
  }
  let nestedForm = form;
  const keys = key.split(".");
  keys.forEach((key2, index) => {
    if (index === keys.length - 1) {
      nestedForm[key2] = value;
    } else {
      if (!nestedForm[key2] || typeof nestedForm[key2] !== "object" || Array.isArray(nestedForm[key2]) || nestedForm[key2] instanceof File) {
        nestedForm[key2] = /* @__PURE__ */ Object.create(null);
      }
      nestedForm = nestedForm[key2];
    }
  });
}, "handleParsingNestedValues");

// ../../../../gacha-audit-20260907/server/node_modules/hono/dist/utils/url.js
var splitPath = /* @__PURE__ */ __name((path) => {
  const paths = path.split("/");
  if (paths[0] === "") {
    paths.shift();
  }
  return paths;
}, "splitPath");
var splitRoutingPath = /* @__PURE__ */ __name((routePath) => {
  const { groups, path } = extractGroupsFromPath(routePath);
  const paths = splitPath(path);
  return replaceGroupMarks(paths, groups);
}, "splitRoutingPath");
var extractGroupsFromPath = /* @__PURE__ */ __name((path) => {
  const groups = [];
  path = path.replace(/\{[^}]+\}/g, (match2, index) => {
    const mark = `@${index}`;
    groups.push([mark, match2]);
    return mark;
  });
  return { groups, path };
}, "extractGroupsFromPath");
var replaceGroupMarks = /* @__PURE__ */ __name((paths, groups) => {
  for (let i = groups.length - 1; i >= 0; i--) {
    const [mark] = groups[i];
    for (let j = paths.length - 1; j >= 0; j--) {
      if (paths[j].includes(mark)) {
        paths[j] = paths[j].replace(mark, groups[i][1]);
        break;
      }
    }
  }
  return paths;
}, "replaceGroupMarks");
var patternCache = {};
var getPattern = /* @__PURE__ */ __name((label, next) => {
  if (label === "*") {
    return "*";
  }
  const match2 = label.match(/^\:([^\{\}]+)(?:\{(.+)\})?$/);
  if (match2) {
    const cacheKey = `${label}#${next}`;
    if (!patternCache[cacheKey]) {
      if (match2[2]) {
        patternCache[cacheKey] = next && next[0] !== ":" && next[0] !== "*" ? [cacheKey, match2[1], new RegExp(`^${match2[2]}(?=/${next})`)] : [label, match2[1], new RegExp(`^${match2[2]}$`)];
      } else {
        patternCache[cacheKey] = [label, match2[1], true];
      }
    }
    return patternCache[cacheKey];
  }
  return null;
}, "getPattern");
var tryDecode = /* @__PURE__ */ __name((str, decoder) => {
  try {
    return decoder(str);
  } catch {
    return str.replace(/(?:%[0-9A-Fa-f]{2})+/g, (match2) => {
      try {
        return decoder(match2);
      } catch {
        return match2;
      }
    });
  }
}, "tryDecode");
var tryDecodeURI = /* @__PURE__ */ __name((str) => tryDecode(str, decodeURI), "tryDecodeURI");
var getPath = /* @__PURE__ */ __name((request) => {
  const url = request.url;
  const start = url.indexOf("/", url.indexOf(":") + 4);
  let i = start;
  for (; i < url.length; i++) {
    const charCode = url.charCodeAt(i);
    if (charCode === 37) {
      const queryIndex = url.indexOf("?", i);
      const hashIndex = url.indexOf("#", i);
      const end = queryIndex === -1 ? hashIndex === -1 ? void 0 : hashIndex : hashIndex === -1 ? queryIndex : Math.min(queryIndex, hashIndex);
      const path = url.slice(start, end);
      return tryDecodeURI(path.includes("%25") ? path.replace(/%25/g, "%2525") : path);
    } else if (charCode === 63 || charCode === 35) {
      break;
    }
  }
  return url.slice(start, i);
}, "getPath");
var getPathNoStrict = /* @__PURE__ */ __name((request) => {
  const result = getPath(request);
  return result.length > 1 && result.at(-1) === "/" ? result.slice(0, -1) : result;
}, "getPathNoStrict");
var mergePath = /* @__PURE__ */ __name((base, sub, ...rest) => {
  if (rest.length) {
    sub = mergePath(sub, ...rest);
  }
  return `${base?.[0] === "/" ? "" : "/"}${base}${sub === "/" ? "" : `${base?.at(-1) === "/" ? "" : "/"}${sub?.[0] === "/" ? sub.slice(1) : sub}`}`;
}, "mergePath");
var checkOptionalParameter = /* @__PURE__ */ __name((path) => {
  if (path.charCodeAt(path.length - 1) !== 63 || !path.includes(":")) {
    return null;
  }
  const segments = path.split("/");
  const results = [];
  let basePath = "";
  segments.forEach((segment) => {
    if (segment !== "" && !/\:/.test(segment)) {
      basePath += "/" + segment;
    } else if (/\:/.test(segment)) {
      if (segment.charCodeAt(segment.length - 1) === 63) {
        if (results.length === 0 && basePath === "") {
          results.push("/");
        } else {
          results.push(basePath);
        }
        const optionalSegment = segment.slice(0, -1);
        basePath += "/" + optionalSegment;
        results.push(basePath);
      } else {
        basePath += "/" + segment;
      }
    }
  });
  return results.filter((v, i, a) => a.indexOf(v) === i);
}, "checkOptionalParameter");
var tryDecodeURIComponent = /* @__PURE__ */ __name((str) => str.indexOf("%") !== -1 ? tryDecode(str, decodeURIComponent_) : str, "tryDecodeURIComponent");
var _decodeURI = /* @__PURE__ */ __name((value) => {
  if (value.indexOf("+") !== -1) {
    value = value.replace(/\+/g, " ");
  }
  return tryDecodeURIComponent(value);
}, "_decodeURI");
var _getQueryParam = /* @__PURE__ */ __name((url, key, multiple) => {
  let encoded;
  if (!multiple && key && key.indexOf("%") === -1 && key.indexOf("+") === -1) {
    let keyIndex2 = url.indexOf("?", 8);
    if (keyIndex2 === -1) {
      return void 0;
    }
    if (!url.startsWith(key, keyIndex2 + 1)) {
      keyIndex2 = url.indexOf(`&${key}`, keyIndex2 + 1);
    }
    while (keyIndex2 !== -1) {
      const trailingKeyCode = url.charCodeAt(keyIndex2 + key.length + 1);
      if (trailingKeyCode === 61) {
        const valueIndex = keyIndex2 + key.length + 2;
        const endIndex = url.indexOf("&", valueIndex);
        return _decodeURI(url.slice(valueIndex, endIndex === -1 ? void 0 : endIndex));
      } else if (trailingKeyCode == 38 || isNaN(trailingKeyCode)) {
        return "";
      }
      keyIndex2 = url.indexOf(`&${key}`, keyIndex2 + 1);
    }
    encoded = /[%+]/.test(url);
    if (!encoded) {
      return void 0;
    }
  }
  const results = /* @__PURE__ */ Object.create(null);
  encoded ??= /[%+]/.test(url);
  let keyIndex = url.indexOf("?", 8);
  while (keyIndex !== -1) {
    const nextKeyIndex = url.indexOf("&", keyIndex + 1);
    let valueIndex = url.indexOf("=", keyIndex);
    if (valueIndex > nextKeyIndex && nextKeyIndex !== -1) {
      valueIndex = -1;
    }
    let name = url.slice(
      keyIndex + 1,
      valueIndex === -1 ? nextKeyIndex === -1 ? void 0 : nextKeyIndex : valueIndex
    );
    if (encoded) {
      name = _decodeURI(name);
    }
    keyIndex = nextKeyIndex;
    if (name === "") {
      continue;
    }
    let value;
    if (valueIndex === -1) {
      value = "";
    } else {
      value = url.slice(valueIndex + 1, nextKeyIndex === -1 ? void 0 : nextKeyIndex);
      if (encoded) {
        value = _decodeURI(value);
      }
    }
    if (multiple) {
      if (!(results[name] && Array.isArray(results[name]))) {
        results[name] = [];
      }
      ;
      results[name].push(value);
    } else {
      results[name] ??= value;
    }
  }
  return key ? results[key] : results;
}, "_getQueryParam");
var getQueryParam = _getQueryParam;
var getQueryParams = /* @__PURE__ */ __name((url, key) => {
  return _getQueryParam(url, key, true);
}, "getQueryParams");
var decodeURIComponent_ = decodeURIComponent;

// ../../../../gacha-audit-20260907/server/node_modules/hono/dist/request.js
var HonoRequest = class {
  static {
    __name(this, "HonoRequest");
  }
  /**
   * `.raw` can get the raw Request object.
   *
   * @see {@link https://hono.dev/docs/api/request#raw}
   *
   * @example
   * ```ts
   * // For Cloudflare Workers
   * app.post('/', async (c) => {
   *   const metadata = c.req.raw.cf?.hostMetadata?
   *   ...
   * })
   * ```
   */
  raw;
  #validatedData;
  // Short name of validatedData
  #matchResult;
  routeIndex = 0;
  /**
   * `.path` can get the pathname of the request.
   *
   * @see {@link https://hono.dev/docs/api/request#path}
   *
   * @example
   * ```ts
   * app.get('/about/me', (c) => {
   *   const pathname = c.req.path // `/about/me`
   * })
   * ```
   */
  path;
  bodyCache = {};
  constructor(request, path = "/", matchResult = [[]]) {
    this.raw = request;
    this.path = path;
    this.#matchResult = matchResult;
  }
  param(key) {
    return key ? this.#getDecodedParam(key) : this.#getAllDecodedParams();
  }
  #getDecodedParam(key) {
    const paramKey = this.#matchResult[0][this.routeIndex][1][key];
    const param = this.#getParamValue(paramKey);
    return param && tryDecodeURIComponent(param);
  }
  #getAllDecodedParams() {
    const decoded = {};
    const keys = Object.keys(this.#matchResult[0][this.routeIndex][1]);
    for (const key of keys) {
      const value = this.#getParamValue(this.#matchResult[0][this.routeIndex][1][key]);
      if (value !== void 0) {
        decoded[key] = tryDecodeURIComponent(value);
      }
    }
    return decoded;
  }
  #getParamValue(paramKey) {
    return this.#matchResult[1] ? this.#matchResult[1][paramKey] : paramKey;
  }
  query(key) {
    return getQueryParam(this.url, key);
  }
  queries(key) {
    return getQueryParams(this.url, key);
  }
  header(name) {
    if (name) {
      return this.raw.headers.get(name) ?? void 0;
    }
    const headerData = /* @__PURE__ */ Object.create(null);
    this.raw.headers.forEach((value, key) => {
      headerData[key] = value;
    });
    return headerData;
  }
  async parseBody(options) {
    return parseBody(this, options);
  }
  #cachedBody = /* @__PURE__ */ __name((key) => {
    const { bodyCache, raw: raw2 } = this;
    const cachedBody = bodyCache[key];
    if (cachedBody) {
      return cachedBody;
    }
    for (const anyCachedKey in bodyCache) {
      return bodyCache[anyCachedKey].then((body) => {
        if (anyCachedKey === "json") {
          body = JSON.stringify(body);
        }
        return new Response(body)[key]();
      });
    }
    return bodyCache[key] = raw2[key]();
  }, "#cachedBody");
  /**
   * `.json()` can parse Request body of type `application/json`
   *
   * @see {@link https://hono.dev/docs/api/request#json}
   *
   * @example
   * ```ts
   * app.post('/entry', async (c) => {
   *   const body = await c.req.json()
   * })
   * ```
   */
  json() {
    return this.#cachedBody("text").then((text) => JSON.parse(text));
  }
  /**
   * `.text()` can parse Request body of type `text/plain`
   *
   * @see {@link https://hono.dev/docs/api/request#text}
   *
   * @example
   * ```ts
   * app.post('/entry', async (c) => {
   *   const body = await c.req.text()
   * })
   * ```
   */
  text() {
    return this.#cachedBody("text");
  }
  /**
   * `.arrayBuffer()` parse Request body as an `ArrayBuffer`
   *
   * @see {@link https://hono.dev/docs/api/request#arraybuffer}
   *
   * @example
   * ```ts
   * app.post('/entry', async (c) => {
   *   const body = await c.req.arrayBuffer()
   * })
   * ```
   */
  arrayBuffer() {
    return this.#cachedBody("arrayBuffer");
  }
  /**
   * `.bytes()` parses the request body as a `Uint8Array`.
   *
   * @see {@link https://hono.dev/docs/api/request#bytes}
   *
   * @example
   * ```ts
   * app.post('/entry', async (c) => {
   *   const body = await c.req.bytes()
   * })
   * ```
   */
  bytes() {
    return this.#cachedBody("arrayBuffer").then((buffer) => new Uint8Array(buffer));
  }
  /**
   * Parses the request body as a `Blob`.
   * @example
   * ```ts
   * app.post('/entry', async (c) => {
   *   const body = await c.req.blob();
   * });
   * ```
   * @see https://hono.dev/docs/api/request#blob
   */
  blob() {
    return this.#cachedBody("blob");
  }
  /**
   * Parses the request body as `FormData`.
   * @example
   * ```ts
   * app.post('/entry', async (c) => {
   *   const body = await c.req.formData();
   * });
   * ```
   * @see https://hono.dev/docs/api/request#formdata
   */
  formData() {
    return this.#cachedBody("formData");
  }
  /**
   * Adds validated data to the request.
   *
   * @param target - The target of the validation.
   * @param data - The validated data to add.
   */
  addValidatedData(target, data) {
    ;
    (this.#validatedData ??= {})[target] = data;
  }
  valid(target) {
    return this.#validatedData?.[target];
  }
  /**
   * `.url()` can get the request url strings.
   *
   * @see {@link https://hono.dev/docs/api/request#url}
   *
   * @example
   * ```ts
   * app.get('/about/me', (c) => {
   *   const url = c.req.url // `http://localhost:8787/about/me`
   *   ...
   * })
   * ```
   */
  get url() {
    return this.raw.url;
  }
  /**
   * `.method()` can get the method name of the request.
   *
   * @see {@link https://hono.dev/docs/api/request#method}
   *
   * @example
   * ```ts
   * app.get('/about/me', (c) => {
   *   const method = c.req.method // `GET`
   * })
   * ```
   */
  get method() {
    return this.raw.method;
  }
  get [GET_MATCH_RESULT]() {
    return this.#matchResult;
  }
  /**
   * `.matchedRoutes()` can return a matched route in the handler
   *
   * @deprecated
   *
   * Use matchedRoutes helper defined in "hono/route" instead.
   *
   * @see {@link https://hono.dev/docs/api/request#matchedroutes}
   *
   * @example
   * ```ts
   * app.use('*', async function logger(c, next) {
   *   await next()
   *   c.req.matchedRoutes.forEach(({ handler, method, path }, i) => {
   *     const name = handler.name || (handler.length < 2 ? '[handler]' : '[middleware]')
   *     console.log(
   *       method,
   *       ' ',
   *       path,
   *       ' '.repeat(Math.max(10 - path.length, 0)),
   *       name,
   *       i === c.req.routeIndex ? '<- respond from here' : ''
   *     )
   *   })
   * })
   * ```
   */
  get matchedRoutes() {
    return this.#matchResult[0].map(([[, route]]) => route);
  }
  /**
   * `routePath()` can retrieve the path registered within the handler
   *
   * @deprecated
   *
   * Use routePath helper defined in "hono/route" instead.
   *
   * @see {@link https://hono.dev/docs/api/request#routepath}
   *
   * @example
   * ```ts
   * app.get('/posts/:id', (c) => {
   *   return c.json({ path: c.req.routePath })
   * })
   * ```
   */
  get routePath() {
    return this.#matchResult[0].map(([[, route]]) => route)[this.routeIndex].path;
  }
};

// ../../../../gacha-audit-20260907/server/node_modules/hono/dist/utils/html.js
var HtmlEscapedCallbackPhase = {
  Stringify: 1,
  BeforeStream: 2,
  Stream: 3
};
var raw = /* @__PURE__ */ __name((value, callbacks) => {
  const escapedString = new String(value);
  escapedString.isEscaped = true;
  escapedString.callbacks = callbacks;
  return escapedString;
}, "raw");
var resolveCallback = /* @__PURE__ */ __name(async (str, phase, preserveCallbacks, context, buffer) => {
  if (typeof str === "object" && !(str instanceof String)) {
    if (!(str instanceof Promise)) {
      str = str.toString();
    }
    if (str instanceof Promise) {
      str = await str;
    }
  }
  const callbacks = str.callbacks;
  if (!callbacks?.length) {
    return Promise.resolve(str);
  }
  if (buffer) {
    buffer[0] += str;
  } else {
    buffer = [str];
  }
  const resStr = Promise.all(callbacks.map((c) => c({ phase, buffer, context }))).then(
    (res) => Promise.all(
      res.filter(Boolean).map((str2) => resolveCallback(str2, phase, false, context, buffer))
    ).then(() => buffer[0])
  );
  if (preserveCallbacks) {
    return raw(await resStr, callbacks);
  } else {
    return resStr;
  }
}, "resolveCallback");

// ../../../../gacha-audit-20260907/server/node_modules/hono/dist/context.js
var TEXT_PLAIN = "text/plain; charset=UTF-8";
var setDefaultContentType = /* @__PURE__ */ __name((contentType, headers) => {
  return {
    "Content-Type": contentType,
    ...headers
  };
}, "setDefaultContentType");
var createResponseInstance = /* @__PURE__ */ __name((body, init) => new Response(body, init), "createResponseInstance");
var Context = class {
  static {
    __name(this, "Context");
  }
  #rawRequest;
  #req;
  /**
   * `.env` can get bindings (environment variables, secrets, KV namespaces, D1 database, R2 bucket etc.) in Cloudflare Workers.
   *
   * @see {@link https://hono.dev/docs/api/context#env}
   *
   * @example
   * ```ts
   * // Environment object for Cloudflare Workers
   * app.get('*', async c => {
   *   const counter = c.env.COUNTER
   * })
   * ```
   */
  env = {};
  #var;
  finalized = false;
  /**
   * `.error` can get the error object from the middleware if the Handler throws an error.
   *
   * @see {@link https://hono.dev/docs/api/context#error}
   *
   * @example
   * ```ts
   * app.use('*', async (c, next) => {
   *   await next()
   *   if (c.error) {
   *     // do something...
   *   }
   * })
   * ```
   */
  error;
  #status;
  #executionCtx;
  #res;
  #layout;
  #renderer;
  #notFoundHandler;
  #preparedHeaders;
  #matchResult;
  #path;
  /**
   * Creates an instance of the Context class.
   *
   * @param req - The Request object.
   * @param options - Optional configuration options for the context.
   */
  constructor(req, options) {
    this.#rawRequest = req;
    if (options) {
      this.#executionCtx = options.executionCtx;
      this.env = options.env;
      this.#notFoundHandler = options.notFoundHandler;
      this.#path = options.path;
      this.#matchResult = options.matchResult;
    }
  }
  /**
   * `.req` is the instance of {@link HonoRequest}.
   */
  get req() {
    this.#req ??= new HonoRequest(this.#rawRequest, this.#path, this.#matchResult);
    return this.#req;
  }
  /**
   * @see {@link https://hono.dev/docs/api/context#event}
   * The FetchEvent associated with the current request.
   *
   * @throws Will throw an error if the context does not have a FetchEvent.
   */
  get event() {
    if (this.#executionCtx && "respondWith" in this.#executionCtx) {
      return this.#executionCtx;
    } else {
      throw Error("This context has no FetchEvent");
    }
  }
  /**
   * @see {@link https://hono.dev/docs/api/context#executionctx}
   * The ExecutionContext associated with the current request.
   *
   * @throws Will throw an error if the context does not have an ExecutionContext.
   */
  get executionCtx() {
    if (this.#executionCtx) {
      return this.#executionCtx;
    } else {
      throw Error("This context has no ExecutionContext");
    }
  }
  /**
   * @see {@link https://hono.dev/docs/api/context#res}
   * The Response object for the current request.
   */
  get res() {
    return this.#res ||= createResponseInstance(null, {
      headers: this.#preparedHeaders ??= new Headers()
    });
  }
  /**
   * Sets the Response object for the current request.
   *
   * @param _res - The Response object to set.
   */
  set res(_res) {
    if (this.#res && _res) {
      _res = createResponseInstance(_res.body, _res);
      for (const [k, v] of this.#res.headers.entries()) {
        if (k === "content-type") {
          continue;
        }
        if (k === "set-cookie") {
          const cookies = this.#res.headers.getSetCookie();
          _res.headers.delete("set-cookie");
          for (const cookie of cookies) {
            _res.headers.append("set-cookie", cookie);
          }
        } else {
          _res.headers.set(k, v);
        }
      }
    }
    this.#res = _res;
    this.finalized = true;
  }
  /**
   * `.render()` can create a response within a layout.
   *
   * @see {@link https://hono.dev/docs/api/context#render-setrenderer}
   *
   * @example
   * ```ts
   * app.get('/', (c) => {
   *   return c.render('Hello!')
   * })
   * ```
   */
  render = /* @__PURE__ */ __name((...args) => {
    this.#renderer ??= (content) => this.html(content);
    return this.#renderer(...args);
  }, "render");
  /**
   * Sets the layout for the response.
   *
   * @param layout - The layout to set.
   * @returns The layout function.
   */
  setLayout = /* @__PURE__ */ __name((layout) => this.#layout = layout, "setLayout");
  /**
   * Gets the current layout for the response.
   *
   * @returns The current layout function.
   */
  getLayout = /* @__PURE__ */ __name(() => this.#layout, "getLayout");
  /**
   * `.setRenderer()` can set the layout in the custom middleware.
   *
   * @see {@link https://hono.dev/docs/api/context#render-setrenderer}
   *
   * @example
   * ```tsx
   * app.use('*', async (c, next) => {
   *   c.setRenderer((content) => {
   *     return c.html(
   *       <html>
   *         <body>
   *           <p>{content}</p>
   *         </body>
   *       </html>
   *     )
   *   })
   *   await next()
   * })
   * ```
   */
  setRenderer = /* @__PURE__ */ __name((renderer) => {
    this.#renderer = renderer;
  }, "setRenderer");
  /**
   * `.header()` can set headers.
   *
   * @see {@link https://hono.dev/docs/api/context#header}
   *
   * @example
   * ```ts
   * app.get('/welcome', (c) => {
   *   // Set headers
   *   c.header('X-Message', 'Hello!')
   *   c.header('Content-Type', 'text/plain')
   *
   *   // Append multiple headers using the append option (e.g. Vary)
   *   c.header('Vary', 'Accept-Encoding', { append: true })
   *   c.header('Vary', 'User-Agent', { append: true })
   *
   *   return c.body('Thank you for coming')
   * })
   * ```
   */
  header = /* @__PURE__ */ __name((name, value, options) => {
    if (this.finalized) {
      this.#res = createResponseInstance(this.#res.body, this.#res);
    }
    const headers = this.#res ? this.#res.headers : this.#preparedHeaders ??= new Headers();
    if (value === void 0) {
      headers.delete(name);
    } else if (options?.append) {
      headers.append(name, value);
    } else {
      headers.set(name, value);
    }
  }, "header");
  status = /* @__PURE__ */ __name((status) => {
    this.#status = status;
  }, "status");
  /**
   * `.set()` can set the value specified by the key.
   *
   * @see {@link https://hono.dev/docs/api/context#set-get}
   *
   * @example
   * ```ts
   * app.use('*', async (c, next) => {
   *   c.set('message', 'Hono is hot!!')
   *   await next()
   * })
   * ```
   */
  set = /* @__PURE__ */ __name((key, value) => {
    this.#var ??= /* @__PURE__ */ new Map();
    this.#var.set(key, value);
  }, "set");
  /**
   * `.get()` can use the value specified by the key.
   *
   * @see {@link https://hono.dev/docs/api/context#set-get}
   *
   * @example
   * ```ts
   * app.get('/', (c) => {
   *   const message = c.get('message')
   *   return c.text(`The message is "${message}"`)
   * })
   * ```
   */
  get = /* @__PURE__ */ __name((key) => {
    return this.#var ? this.#var.get(key) : void 0;
  }, "get");
  /**
   * `.var` can access the value of a variable.
   *
   * @see {@link https://hono.dev/docs/api/context#var}
   *
   * @example
   * ```ts
   * const result = c.var.client.oneMethod()
   * ```
   */
  // c.var.propName is a read-only
  get var() {
    if (!this.#var) {
      return {};
    }
    return Object.fromEntries(this.#var);
  }
  #newResponse(data, arg, headers) {
    let responseHeaders = this.#res ? new Headers(this.#res.headers) : this.#preparedHeaders;
    if (typeof arg === "object" && arg.headers) {
      responseHeaders ??= new Headers();
      for (const [key, value] of new Headers(arg.headers)) {
        if (key === "set-cookie") {
          responseHeaders.append(key, value);
        } else {
          responseHeaders.set(key, value);
        }
      }
    }
    if (headers) {
      if (!responseHeaders) {
        let count = 0;
        for (const k in headers) {
          if (++count > 1 || typeof headers[k] !== "string") {
            responseHeaders = new Headers();
            break;
          }
        }
      }
      if (responseHeaders) {
        for (const k in headers) {
          const v = headers[k];
          if (typeof v === "string") {
            responseHeaders.set(k, v);
          } else {
            responseHeaders.delete(k);
            for (const v2 of v) {
              responseHeaders.append(k, v2);
            }
          }
        }
      }
    }
    const status = typeof arg === "number" ? arg : arg?.status ?? this.#status;
    return createResponseInstance(data, {
      status,
      headers: responseHeaders ?? headers
    });
  }
  newResponse = /* @__PURE__ */ __name((...args) => this.#newResponse(...args), "newResponse");
  /**
   * `.body()` can return the HTTP response.
   * You can set headers with `.header()` and set HTTP status code with `.status`.
   * This can also be set in `.text()`, `.json()` and so on.
   *
   * @see {@link https://hono.dev/docs/api/context#body}
   *
   * @example
   * ```ts
   * app.get('/welcome', (c) => {
   *   // Set headers
   *   c.header('X-Message', 'Hello!')
   *   c.header('Content-Type', 'text/plain')
   *   // Set HTTP status code
   *   c.status(201)
   *
   *   // Return the response body
   *   return c.body('Thank you for coming')
   * })
   * ```
   */
  body = /* @__PURE__ */ __name((data, arg, headers) => this.#newResponse(data, arg, headers), "body");
  /**
   * `.text()` can render text as `Content-Type:text/plain`.
   *
   * @see {@link https://hono.dev/docs/api/context#text}
   *
   * @example
   * ```ts
   * app.get('/say', (c) => {
   *   return c.text('Hello!')
   * })
   * ```
   */
  text = /* @__PURE__ */ __name((text, arg, headers) => {
    return !this.#preparedHeaders && !this.#status && !arg && !headers && !this.finalized ? new Response(text) : this.#newResponse(
      text,
      arg,
      setDefaultContentType(TEXT_PLAIN, headers)
    );
  }, "text");
  /**
   * `.json()` can render JSON as `Content-Type:application/json`.
   *
   * @see {@link https://hono.dev/docs/api/context#json}
   *
   * @example
   * ```ts
   * app.get('/api', (c) => {
   *   return c.json({ message: 'Hello!' })
   * })
   * ```
   */
  json = /* @__PURE__ */ __name((object, arg, headers) => {
    return this.#newResponse(
      JSON.stringify(object),
      arg,
      setDefaultContentType("application/json", headers)
    );
  }, "json");
  html = /* @__PURE__ */ __name((html, arg, headers) => {
    const res = /* @__PURE__ */ __name((html2) => this.#newResponse(html2, arg, setDefaultContentType("text/html; charset=UTF-8", headers)), "res");
    return typeof html === "object" ? resolveCallback(html, HtmlEscapedCallbackPhase.Stringify, false, {}).then(res) : res(html);
  }, "html");
  /**
   * `.redirect()` can Redirect, default status code is 302.
   *
   * @see {@link https://hono.dev/docs/api/context#redirect}
   *
   * @example
   * ```ts
   * app.get('/redirect', (c) => {
   *   return c.redirect('/')
   * })
   * app.get('/redirect-permanently', (c) => {
   *   return c.redirect('/', 301)
   * })
   * ```
   */
  redirect = /* @__PURE__ */ __name((location, status) => {
    const locationString = String(location);
    this.header(
      "Location",
      // Multibyes should be encoded
      // eslint-disable-next-line no-control-regex
      !/[^\x00-\xFF]/.test(locationString) ? locationString : encodeURI(locationString)
    );
    return this.newResponse(null, status ?? 302);
  }, "redirect");
  /**
   * `.notFound()` can return the Not Found Response.
   *
   * @see {@link https://hono.dev/docs/api/context#notfound}
   *
   * @example
   * ```ts
   * app.get('/notfound', (c) => {
   *   return c.notFound()
   * })
   * ```
   */
  notFound = /* @__PURE__ */ __name(() => {
    this.#notFoundHandler ??= () => createResponseInstance();
    return this.#notFoundHandler(this);
  }, "notFound");
};

// ../../../../gacha-audit-20260907/server/node_modules/hono/dist/router.js
var METHOD_NAME_ALL = "ALL";
var METHOD_NAME_ALL_LOWERCASE = "all";
var METHODS = ["get", "post", "put", "delete", "options", "patch", "query"];
var MESSAGE_MATCHER_IS_ALREADY_BUILT = "Can not add a route since the matcher is already built.";
var UnsupportedPathError = class extends Error {
  static {
    __name(this, "UnsupportedPathError");
  }
};

// ../../../../gacha-audit-20260907/server/node_modules/hono/dist/utils/constants.js
var COMPOSED_HANDLER = "__COMPOSED_HANDLER";

// ../../../../gacha-audit-20260907/server/node_modules/hono/dist/hono-base.js
var notFoundHandler = /* @__PURE__ */ __name((c) => {
  return c.text("404 Not Found", 404);
}, "notFoundHandler");
var errorHandler = /* @__PURE__ */ __name((err, c) => {
  if ("getResponse" in err) {
    const res = err.getResponse();
    return c.newResponse(res.body, res);
  }
  console.error(err);
  return c.text("Internal Server Error", 500);
}, "errorHandler");
var Hono = class _Hono {
  static {
    __name(this, "_Hono");
  }
  get;
  post;
  put;
  delete;
  options;
  patch;
  query;
  all;
  on;
  use;
  /*
    This class is like an abstract class and does not have a router.
    To use it, inherit the class and implement router in the constructor.
  */
  router;
  getPath;
  // Cannot use `#` because it requires visibility at JavaScript runtime.
  _basePath = "/";
  #path = "/";
  routes = [];
  constructor(options = {}) {
    const allMethods = [...METHODS, METHOD_NAME_ALL_LOWERCASE];
    allMethods.forEach((method) => {
      this[method] = (args1, ...args) => {
        if (typeof args1 === "string") {
          this.#path = args1;
        } else {
          this.#addRoute(method, this.#path, args1);
        }
        args.forEach((handler) => {
          this.#addRoute(method, this.#path, handler);
        });
        return this;
      };
    });
    this.on = (method, path, ...handlers) => {
      for (const p of [path].flat()) {
        this.#path = p;
        for (const m of [method].flat()) {
          handlers.map((handler) => {
            this.#addRoute(m.toUpperCase(), this.#path, handler);
          });
        }
      }
      return this;
    };
    this.use = (arg1, ...handlers) => {
      if (typeof arg1 === "string") {
        this.#path = arg1;
      } else {
        this.#path = "*";
        handlers.unshift(arg1);
      }
      handlers.forEach((handler) => {
        this.#addRoute(METHOD_NAME_ALL, this.#path, handler);
      });
      return this;
    };
    const { strict, ...optionsWithoutStrict } = options;
    Object.assign(this, optionsWithoutStrict);
    this.getPath = strict ?? true ? options.getPath ?? getPath : getPathNoStrict;
  }
  #clone() {
    const clone = new _Hono({
      router: this.router,
      getPath: this.getPath
    });
    clone.errorHandler = this.errorHandler;
    clone.#notFoundHandler = this.#notFoundHandler;
    clone.routes = this.routes;
    return clone;
  }
  #notFoundHandler = notFoundHandler;
  // Cannot use `#` because it requires visibility at JavaScript runtime.
  errorHandler = errorHandler;
  /**
   * `.route()` allows grouping other Hono instance in routes.
   *
   * @see {@link https://hono.dev/docs/api/routing#grouping}
   *
   * @param {string} path - base Path
   * @param {Hono} app - other Hono instance
   * @returns {Hono} routed Hono instance
   *
   * @example
   * ```ts
   * const app = new Hono()
   * const app2 = new Hono()
   *
   * app2.get("/user", (c) => c.text("user"))
   * app.route("/api", app2) // GET /api/user
   * ```
   */
  route(path, app2) {
    const subApp = this.basePath(path);
    app2.routes.map((r) => {
      let handler;
      if (app2.errorHandler === errorHandler) {
        handler = r.handler;
      } else {
        handler = /* @__PURE__ */ __name(async (c, next) => (await compose([], app2.errorHandler)(c, () => r.handler(c, next))).res, "handler");
        handler[COMPOSED_HANDLER] = r.handler;
      }
      subApp.#addRoute(r.method, r.path, handler, r.basePath);
    });
    return this;
  }
  /**
   * `.basePath()` allows base paths to be specified.
   *
   * @see {@link https://hono.dev/docs/api/routing#base-path}
   *
   * @param {string} path - base Path
   * @returns {Hono} changed Hono instance
   *
   * @example
   * ```ts
   * const api = new Hono().basePath('/api')
   * ```
   */
  basePath(path) {
    const subApp = this.#clone();
    subApp._basePath = mergePath(this._basePath, path);
    return subApp;
  }
  /**
   * `.onError()` handles an error and returns a customized Response.
   *
   * @see {@link https://hono.dev/docs/api/hono#error-handling}
   *
   * @param {ErrorHandler} handler - request Handler for error
   * @returns {Hono} changed Hono instance
   *
   * @example
   * ```ts
   * app.onError((err, c) => {
   *   console.error(`${err}`)
   *   return c.text('Custom Error Message', 500)
   * })
   * ```
   */
  onError = /* @__PURE__ */ __name((handler) => {
    this.errorHandler = handler;
    return this;
  }, "onError");
  /**
   * `.notFound()` allows you to customize a Not Found Response.
   *
   * @see {@link https://hono.dev/docs/api/hono#not-found}
   *
   * @param {NotFoundHandler} handler - request handler for not-found
   * @returns {Hono} changed Hono instance
   *
   * @example
   * ```ts
   * app.notFound((c) => {
   *   return c.text('Custom 404 Message', 404)
   * })
   * ```
   */
  notFound = /* @__PURE__ */ __name((handler) => {
    this.#notFoundHandler = handler;
    return this;
  }, "notFound");
  /**
   * `.mount()` allows you to mount applications built with other frameworks into your Hono application.
   *
   * @see {@link https://hono.dev/docs/api/hono#mount}
   *
   * @param {string} path - base Path
   * @param {Function} applicationHandler - other Request Handler
   * @param {MountOptions} [options] - options of `.mount()`
   * @returns {Hono} mounted Hono instance
   *
   * @example
   * ```ts
   * import { Router as IttyRouter } from 'itty-router'
   * import { Hono } from 'hono'
   * // Create itty-router application
   * const ittyRouter = IttyRouter()
   * // GET /itty-router/hello
   * ittyRouter.get('/hello', () => new Response('Hello from itty-router'))
   *
   * const app = new Hono()
   * app.mount('/itty-router', ittyRouter.handle)
   * ```
   *
   * @example
   * ```ts
   * const app = new Hono()
   * // Send the request to another application without modification.
   * app.mount('/app', anotherApp, {
   *   replaceRequest: (req) => req,
   * })
   * ```
   */
  mount(path, applicationHandler, options) {
    let replaceRequest;
    let optionHandler;
    if (options) {
      if (typeof options === "function") {
        optionHandler = options;
      } else {
        optionHandler = options.optionHandler;
        if (options.replaceRequest === false) {
          replaceRequest = /* @__PURE__ */ __name((request) => request, "replaceRequest");
        } else {
          replaceRequest = options.replaceRequest;
        }
      }
    }
    const getOptions = optionHandler ? (c) => {
      const options2 = optionHandler(c);
      return Array.isArray(options2) ? options2 : [options2];
    } : (c) => {
      let executionContext = void 0;
      try {
        executionContext = c.executionCtx;
      } catch {
      }
      return [c.env, executionContext];
    };
    replaceRequest ||= (() => {
      const mergedPath = mergePath(this._basePath, path);
      const pathPrefixLength = mergedPath === "/" ? 0 : mergedPath.length;
      return (request) => {
        const url = new URL(request.url);
        url.pathname = this.getPath(request).slice(pathPrefixLength) || "/";
        return new Request(url, request);
      };
    })();
    const handler = /* @__PURE__ */ __name(async (c, next) => {
      const res = await applicationHandler(replaceRequest(c.req.raw), ...getOptions(c));
      if (res) {
        return res;
      }
      await next();
    }, "handler");
    this.#addRoute(METHOD_NAME_ALL, mergePath(path, "*"), handler);
    return this;
  }
  #addRoute(method, path, handler, baseRoutePath) {
    method = method.toUpperCase();
    path = mergePath(this._basePath, path);
    const r = {
      basePath: baseRoutePath !== void 0 ? mergePath(this._basePath, baseRoutePath) : this._basePath,
      path,
      method,
      handler
    };
    this.router.add(method, path, [handler, r]);
    this.routes.push(r);
  }
  #handleError(err, c) {
    if (err instanceof Error) {
      return this.errorHandler(err, c);
    }
    throw err;
  }
  #dispatch(request, executionCtx, env, method) {
    if (method === "HEAD") {
      return (async () => new Response(null, await this.#dispatch(request, executionCtx, env, "GET")))();
    }
    const path = this.getPath(request, { env });
    const matchResult = this.router.match(method, path);
    const c = new Context(request, {
      path,
      matchResult,
      env,
      executionCtx,
      notFoundHandler: this.#notFoundHandler
    });
    if (matchResult[0].length === 1) {
      let res;
      try {
        res = matchResult[0][0][0][0](c, async () => {
          c.res = await this.#notFoundHandler(c);
        });
      } catch (err) {
        return this.#handleError(err, c);
      }
      return res instanceof Promise ? res.then(
        (resolved) => resolved || (c.finalized ? c.res : this.#notFoundHandler(c))
      ).catch((err) => this.#handleError(err, c)) : res ?? this.#notFoundHandler(c);
    }
    const composed = compose(matchResult[0], this.errorHandler, this.#notFoundHandler);
    return (async () => {
      try {
        const context = await composed(c);
        if (!context.finalized) {
          throw new Error(
            "Context is not finalized. Did you forget to return a Response object or `await next()`?"
          );
        }
        return context.res;
      } catch (err) {
        return this.#handleError(err, c);
      }
    })();
  }
  /**
   * `.fetch()` will be entry point of your app.
   *
   * @see {@link https://hono.dev/docs/api/hono#fetch}
   *
   * @param {Request} request - request Object of request
   * @param {Env} env - env Object
   * @param {ExecutionContext} executionCtx - context of execution
   * @returns {Response | Promise<Response>} response of request
   *
   */
  fetch = /* @__PURE__ */ __name((request, ...rest) => {
    return this.#dispatch(request, rest[1], rest[0], request.method);
  }, "fetch");
  /**
   * `.request()` is a useful method for testing.
   * You can pass a URL or pathname to send a GET request.
   * app will return a Response object.
   * ```ts
   * test('GET /hello is ok', async () => {
   *   const res = await app.request('/hello')
   *   expect(res.status).toBe(200)
   * })
   * ```
   * @see https://hono.dev/docs/api/hono#request
   */
  request = /* @__PURE__ */ __name((input, requestInit, Env, executionCtx) => {
    if (input instanceof Request) {
      return this.fetch(requestInit ? new Request(input, requestInit) : input, Env, executionCtx);
    }
    input = input.toString();
    return this.fetch(
      new Request(
        /^https?:\/\//.test(input) ? input : `http://localhost${mergePath("/", input)}`,
        requestInit
      ),
      Env,
      executionCtx
    );
  }, "request");
  /**
   * `.fire()` automatically adds a global fetch event listener.
   * This can be useful for environments that adhere to the Service Worker API, such as non-ES module Cloudflare Workers.
   * @deprecated
   * Use `fire` from `hono/service-worker` instead.
   * ```ts
   * import { Hono } from 'hono'
   * import { fire } from 'hono/service-worker'
   *
   * const app = new Hono()
   * // ...
   * fire(app)
   * ```
   * @see https://hono.dev/docs/api/hono#fire
   * @see https://developer.mozilla.org/en-US/docs/Web/API/Service_Worker_API
   * @see https://developers.cloudflare.com/workers/reference/migrate-to-module-workers/
   */
  fire = /* @__PURE__ */ __name(() => {
    addEventListener("fetch", (event) => {
      event.respondWith(this.#dispatch(event.request, event, void 0, event.request.method));
    });
  }, "fire");
};

// ../../../../gacha-audit-20260907/server/node_modules/hono/dist/router/reg-exp-router/matcher.js
var emptyParam = [];
function match(method, path) {
  const matchers = this.buildAllMatchers();
  const match2 = /* @__PURE__ */ __name(((method2, path2) => {
    const matcher = matchers[method2] || matchers[METHOD_NAME_ALL];
    const staticMatch = matcher[2][path2];
    if (staticMatch) {
      return staticMatch;
    }
    const match3 = path2.match(matcher[0]);
    if (!match3) {
      return [[], emptyParam];
    }
    const index = match3.indexOf("", 1);
    return [matcher[1][index], match3];
  }), "match2");
  this.match = match2;
  return match2(method, path);
}
__name(match, "match");

// ../../../../gacha-audit-20260907/server/node_modules/hono/dist/router/reg-exp-router/node.js
var LABEL_REG_EXP_STR = "[^/]+";
var ONLY_WILDCARD_REG_EXP_STR = ".*";
var TAIL_WILDCARD_REG_EXP_STR = "(?:|/.*)";
var PATH_ERROR = /* @__PURE__ */ Symbol();
var regExpMetaChars = new Set(".\\+*[^]$()");
function compareKey(a, b) {
  if (a.length === 1) {
    return b.length === 1 ? a < b ? -1 : 1 : -1;
  }
  if (b.length === 1) {
    return 1;
  }
  if (a === ONLY_WILDCARD_REG_EXP_STR || a === TAIL_WILDCARD_REG_EXP_STR) {
    return b === TAIL_WILDCARD_REG_EXP_STR ? -1 : 1;
  } else if (b === ONLY_WILDCARD_REG_EXP_STR || b === TAIL_WILDCARD_REG_EXP_STR) {
    return -1;
  }
  if (a === LABEL_REG_EXP_STR) {
    return 1;
  } else if (b === LABEL_REG_EXP_STR) {
    return -1;
  }
  return a.length === b.length ? a < b ? -1 : 1 : b.length - a.length;
}
__name(compareKey, "compareKey");
var Node = class _Node {
  static {
    __name(this, "_Node");
  }
  // handler index of a dynamic path, or -1 for a static path terminal
  #index;
  #varIndex;
  #children = /* @__PURE__ */ Object.create(null);
  insert(tokens, index, paramMap, context, isStatic) {
    let node = this;
    for (let i = 0, len = tokens.length; i < len; i++) {
      const token = tokens[i];
      const pattern = token.length === 1 ? token === "*" ? i === len - 1 ? ["", "", ONLY_WILDCARD_REG_EXP_STR] : ["", "", LABEL_REG_EXP_STR] : null : token === "/*" ? ["", "", TAIL_WILDCARD_REG_EXP_STR] : token.match(/^\:([^\{\}]+)(?:\{(.+)\})?$/);
      let nextNode;
      if (pattern) {
        const name = pattern[1];
        let regexpStr = pattern[2] || LABEL_REG_EXP_STR;
        if (name && pattern[2]) {
          if (regexpStr === ".*") {
            throw PATH_ERROR;
          }
          regexpStr = regexpStr.replace(/^\((?!\?:)(?=[^)]+\)$)/, "(?:");
          if (/\((?!\?:)/.test(regexpStr)) {
            throw PATH_ERROR;
          }
          if (regexpStr.length === 1 && regExpMetaChars.has(regexpStr)) {
            throw PATH_ERROR;
          }
        }
        nextNode = node.#children[regexpStr];
        if (!nextNode) {
          if (regexpStr !== ONLY_WILDCARD_REG_EXP_STR && regexpStr !== TAIL_WILDCARD_REG_EXP_STR) {
            for (const k in node.#children) {
              if (
                // a single-char pattern coexists with single-char literals as a literal does
                (regexpStr.length > 1 || k.length > 1) && k !== ONLY_WILDCARD_REG_EXP_STR && k !== TAIL_WILDCARD_REG_EXP_STR
              ) {
                throw PATH_ERROR;
              }
            }
          }
          nextNode = node.#children[regexpStr] = new _Node();
        }
        if (name !== "") {
          nextNode.#varIndex ??= context.varIndex++;
          paramMap.push([name, nextNode.#varIndex]);
        }
      } else {
        nextNode = node.#children[token];
        if (!nextNode) {
          for (const k in node.#children) {
            if (k.length > 1 && k !== ONLY_WILDCARD_REG_EXP_STR && k !== TAIL_WILDCARD_REG_EXP_STR) {
              throw PATH_ERROR;
            }
          }
          nextNode = node.#children[token] = new _Node();
        }
      }
      node = nextNode;
    }
    if (node.#index !== void 0) {
      throw PATH_ERROR;
    }
    node.#index = isStatic ? -1 : index;
  }
  buildRegExpStr() {
    const childKeys = Object.keys(this.#children).sort(compareKey);
    const strList = childKeys.map((k) => {
      const c = this.#children[k];
      const childStr = c.buildRegExpStr();
      return childStr === "" ? "" : (typeof c.#varIndex === "number" ? `(${k})@${c.#varIndex}` : regExpMetaChars.has(k) ? `\\${k}` : k) + childStr;
    }).filter(Boolean);
    if (typeof this.#index === "number" && this.#index !== -1) {
      strList.unshift(`#${this.#index}`);
    }
    if (strList.length === 0) {
      return "";
    }
    if (strList.length === 1) {
      return strList[0];
    }
    return "(?:" + strList.join("|") + ")";
  }
};

// ../../../../gacha-audit-20260907/server/node_modules/hono/dist/router/reg-exp-router/trie.js
var Trie = class {
  static {
    __name(this, "Trie");
  }
  #context = { varIndex: 0 };
  #root = new Node();
  #index = 0;
  // dynamic path -> [handler index, param assoc]; static paths are not registered
  paths = /* @__PURE__ */ Object.create(null);
  insert(path, isStatic) {
    if (isStatic) {
      this.#root.insert(path.split(""), 0, [], this.#context, true);
      return;
    }
    const paramAssoc = [];
    const groups = [];
    let markedPath = path;
    for (let i = 0; ; ) {
      let replaced = false;
      markedPath = markedPath.replace(/\{[^}]+\}/g, (m) => {
        const mark = `@\\${i}`;
        groups[i] = [mark, m];
        i++;
        replaced = true;
        return mark;
      });
      if (!replaced) {
        break;
      }
    }
    const tokens = markedPath.match(/(?::[^\/]+)|(?:\/\*$)|./g) || [];
    for (let i = groups.length - 1; i >= 0; i--) {
      const [mark] = groups[i];
      for (let j = tokens.length - 1; j >= 0; j--) {
        if (tokens[j].indexOf(mark) !== -1) {
          tokens[j] = tokens[j].replace(mark, groups[i][1]);
          break;
        }
      }
    }
    this.#root.insert(tokens, this.#index, paramAssoc, this.#context, false);
    this.paths[path] = [this.#index++, paramAssoc];
  }
  buildRegExp() {
    let regexp = this.#root.buildRegExpStr();
    if (regexp === "") {
      return [/^$/, [], []];
    }
    let captureIndex = 0;
    const indexReplacementMap = [];
    const paramReplacementMap = [];
    regexp = regexp.replace(/#(\d+)|@(\d+)|\.\*\$/g, (_, handlerIndex, paramIndex) => {
      if (handlerIndex !== void 0) {
        indexReplacementMap[++captureIndex] = Number(handlerIndex);
        return "$()";
      }
      if (paramIndex !== void 0) {
        paramReplacementMap[Number(paramIndex)] = ++captureIndex;
        return "";
      }
      return "";
    });
    return [new RegExp(`^${regexp}`), indexReplacementMap, paramReplacementMap];
  }
};

// ../../../../gacha-audit-20260907/server/node_modules/hono/dist/router/reg-exp-router/router.js
var wildcardRegExpCache = /* @__PURE__ */ Object.create(null);
function buildWildcardRegExp(path) {
  return wildcardRegExpCache[path] ??= new RegExp(
    path === "*" ? "" : `^${path.replace(
      /\/\*$|([.\\+*[^\]$()])/g,
      (_, metaChar) => metaChar ? `\\${metaChar}` : "(?:|/.*)"
    )}$`
  );
}
__name(buildWildcardRegExp, "buildWildcardRegExp");
function clearWildcardRegExpCache() {
  wildcardRegExpCache = /* @__PURE__ */ Object.create(null);
}
__name(clearWildcardRegExpCache, "clearWildcardRegExpCache");
function findMiddleware(middleware, path) {
  if (!middleware) {
    return void 0;
  }
  for (const k of Object.keys(middleware).sort((a, b) => b.length - a.length)) {
    if (buildWildcardRegExp(k).test(path)) {
      return [...middleware[k]];
    }
  }
  return void 0;
}
__name(findMiddleware, "findMiddleware");
var RegExpRouter = class {
  static {
    __name(this, "RegExpRouter");
  }
  name = "RegExpRouter";
  #middleware;
  #routes;
  #tries;
  constructor() {
    this.#middleware = { [METHOD_NAME_ALL]: /* @__PURE__ */ Object.create(null) };
    this.#routes = { [METHOD_NAME_ALL]: /* @__PURE__ */ Object.create(null) };
    this.#tries = { [METHOD_NAME_ALL]: new Trie() };
  }
  #insertPath(method, path) {
    try {
      this.#tries[method].insert(path, !/\*|\/:/.test(path));
    } catch (e) {
      throw e === PATH_ERROR ? new UnsupportedPathError(path) : e;
    }
  }
  add(method, path, handler) {
    const middleware = this.#middleware;
    const routes = this.#routes;
    if (!middleware || !routes) {
      throw new Error(MESSAGE_MATCHER_IS_ALREADY_BUILT);
    }
    if (!middleware[method]) {
      this.#tries[method] = new Trie();
      [middleware, routes].forEach((handlerMap) => {
        handlerMap[method] = /* @__PURE__ */ Object.create(null);
        Object.keys(handlerMap[METHOD_NAME_ALL]).forEach((p) => {
          handlerMap[method][p] = [...handlerMap[METHOD_NAME_ALL][p]];
          this.#insertPath(method, p);
        });
      });
    }
    if (path === "/*") {
      path = "*";
    }
    const paramCount = (path.match(/\/:/g) || []).length;
    if (/\*$/.test(path)) {
      const re = buildWildcardRegExp(path);
      Object.keys(middleware).forEach((m) => {
        if ((method === METHOD_NAME_ALL || method === m) && !middleware[m][path]) {
          this.#insertPath(m, path);
          middleware[m][path] = findMiddleware(middleware[m], path) || findMiddleware(middleware[METHOD_NAME_ALL], path) || [];
        }
      });
      Object.keys(middleware).forEach((m) => {
        if (method === METHOD_NAME_ALL || method === m) {
          Object.keys(middleware[m]).forEach((p) => {
            re.test(p) && middleware[m][p].push([handler, paramCount]);
          });
        }
      });
      Object.keys(routes).forEach((m) => {
        if (method === METHOD_NAME_ALL || method === m) {
          Object.keys(routes[m]).forEach(
            (p) => re.test(p) && routes[m][p].push([handler, paramCount])
          );
        }
      });
      return;
    }
    const paths = checkOptionalParameter(path) || [path];
    for (let i = 0, len = paths.length; i < len; i++) {
      const path2 = paths[i];
      Object.keys(routes).forEach((m) => {
        if (method === METHOD_NAME_ALL || method === m) {
          if (!routes[m][path2]) {
            this.#insertPath(m, path2);
            routes[m][path2] = [
              ...findMiddleware(middleware[m], path2) || findMiddleware(middleware[METHOD_NAME_ALL], path2) || []
            ];
          }
          routes[m][path2].push([handler, paramCount - len + i + 1]);
        }
      });
    }
  }
  match = match;
  buildAllMatchers() {
    const matchers = /* @__PURE__ */ Object.create(null);
    Object.keys(this.#routes).concat(Object.keys(this.#middleware)).forEach((method) => {
      matchers[method] ||= this.#buildMatcher(method);
    });
    this.#middleware = this.#routes = this.#tries = void 0;
    clearWildcardRegExpCache();
    return matchers;
  }
  #buildMatcher(method) {
    const middleware = this.#middleware[method];
    const routes = this.#routes[method];
    const trie = this.#tries[method];
    const staticMap = /* @__PURE__ */ Object.create(null);
    const handlerData = [];
    [middleware, routes].forEach((r) => {
      for (const path in r) {
        const handlers = r[path];
        const pathData = trie.paths[path];
        if (!pathData) {
          staticMap[path] = [handlers.map(([h]) => [h, /* @__PURE__ */ Object.create(null)]), emptyParam];
          continue;
        }
        const paramAssoc = pathData[1];
        handlerData[pathData[0]] = handlers.map(([h, paramCount]) => {
          const paramIndexMap = /* @__PURE__ */ Object.create(null);
          paramCount -= 1;
          for (; paramCount >= 0; paramCount--) {
            const [key, value] = paramAssoc[paramCount];
            paramIndexMap[key] = value;
          }
          return [h, paramIndexMap];
        });
      }
    });
    const [regexp, indexReplacementMap, paramReplacementMap] = trie.buildRegExp();
    for (let i = 0, len = handlerData.length; i < len; i++) {
      for (let j = 0, len2 = handlerData[i].length; j < len2; j++) {
        const map = handlerData[i][j]?.[1];
        if (!map) {
          continue;
        }
        const keys = Object.keys(map);
        for (let k = 0, len3 = keys.length; k < len3; k++) {
          map[keys[k]] = paramReplacementMap[map[keys[k]]];
        }
      }
    }
    const handlerMap = [];
    for (const i in indexReplacementMap) {
      handlerMap[i] = handlerData[indexReplacementMap[i]];
    }
    return [regexp, handlerMap, staticMap];
  }
};

// ../../../../gacha-audit-20260907/server/node_modules/hono/dist/router/smart-router/router.js
var SmartRouter = class {
  static {
    __name(this, "SmartRouter");
  }
  name = "SmartRouter";
  #routers = [];
  #routes = [];
  constructor(init) {
    this.#routers = init.routers;
  }
  add(method, path, handler) {
    if (!this.#routes) {
      throw new Error(MESSAGE_MATCHER_IS_ALREADY_BUILT);
    }
    this.#routes.push([method, path, handler]);
  }
  match(method, path) {
    if (!this.#routes) {
      throw new Error("Fatal error");
    }
    const routers = this.#routers;
    const routes = this.#routes;
    const len = routers.length;
    let i = 0;
    let res;
    for (; i < len; i++) {
      const router = routers[i];
      try {
        for (let i2 = 0, len2 = routes.length; i2 < len2; i2++) {
          router.add(...routes[i2]);
        }
        res = router.match(method, path);
      } catch (e) {
        if (e instanceof UnsupportedPathError) {
          continue;
        }
        throw e;
      }
      this.match = router.match.bind(router);
      this.#routers = [router];
      this.#routes = void 0;
      break;
    }
    if (i === len) {
      throw new Error("Fatal error");
    }
    this.name = `SmartRouter + ${this.activeRouter.name}`;
    return res;
  }
  get activeRouter() {
    if (this.#routes || this.#routers.length !== 1) {
      throw new Error("No active router has been determined yet.");
    }
    return this.#routers[0];
  }
};

// ../../../../gacha-audit-20260907/server/node_modules/hono/dist/router/trie-router/node.js
var emptyParams = /* @__PURE__ */ Object.create(null);
var order = 0;
var Node2 = class _Node2 {
  static {
    __name(this, "_Node");
  }
  #methods = [];
  #children = /* @__PURE__ */ Object.create(null);
  #patterns = [];
  #pattern;
  #params = emptyParams;
  insert(method, path, handler) {
    let curNode = this;
    const parts = splitRoutingPath(path);
    const possibleKeys = /* @__PURE__ */ new Set();
    let i = 0;
    for (const p of parts) {
      const nextP = parts[++i];
      const pattern = getPattern(p, nextP) || (nextP === void 0 && p && p.indexOf("*") === p.length - 1 ? p : null);
      const isParam = Array.isArray(pattern);
      const key = isParam ? pattern[0] : pattern || p;
      const child = curNode.#children[key] ||= new _Node2();
      if (pattern && !child.#pattern) {
        child.#pattern = pattern;
        curNode.#patterns.push(child);
      }
      curNode = child;
      if (isParam) {
        possibleKeys.add(pattern[1]);
      }
    }
    curNode.#methods.push({
      [method]: {
        handler,
        possibleKeys: [...possibleKeys],
        score: ++order
      }
    });
  }
  #pushHandlerSets(handlerSets, node, method, nodeParams, params) {
    for (let i = 0, len = node.#methods.length; i < len; i++) {
      const m = node.#methods[i];
      const handlerSet = m[method] || m[METHOD_NAME_ALL];
      if (handlerSet) {
        handlerSet.params = /* @__PURE__ */ Object.create(null);
        handlerSets.push(handlerSet);
        for (let i2 = 0, len2 = handlerSet.possibleKeys.length; i2 < len2; i2++) {
          const key = handlerSet.possibleKeys[i2];
          handlerSet.params[key] = params?.[key] && !i2 ? params[key] : nodeParams[key] ?? params?.[key];
        }
      }
    }
  }
  search(method, path) {
    const handlerSets = [];
    this.#params = emptyParams;
    const curNode = this;
    let curNodes = [curNode];
    const parts = splitPath(path);
    const curNodesQueue = [];
    const len = parts.length;
    let partOffsets = null;
    for (let i = 0; i < len; i++) {
      const part = parts[i];
      const isLast = i === len - 1;
      const tempNodes = [];
      for (let j = 0, len2 = curNodes.length; j < len2; j++) {
        const node = curNodes[j];
        const nextNode = node.#children[part];
        if (nextNode) {
          nextNode.#params = node.#params;
          if (isLast) {
            if (nextNode.#children["*"]) {
              this.#pushHandlerSets(handlerSets, nextNode.#children["*"], method, node.#params);
            }
            this.#pushHandlerSets(handlerSets, nextNode, method, node.#params);
          } else {
            tempNodes.push(nextNode);
          }
        }
        for (const child of node.#patterns) {
          const pattern = child.#pattern;
          const params = node.#params === emptyParams ? {} : { ...node.#params };
          if (typeof pattern === "string") {
            if (pattern === "*" || part.startsWith(pattern.slice(0, -1))) {
              this.#pushHandlerSets(handlerSets, child, method, node.#params);
              if (pattern === "*") {
                child.#params = params;
                tempNodes.push(child);
              }
            }
            continue;
          }
          const [, name, matcher] = pattern;
          if (!part && matcher === true) {
            continue;
          }
          if (matcher !== true) {
            if (!partOffsets) {
              partOffsets = [];
              let offset = path[0] === "/" ? 1 : 0;
              for (let p = 0; p < len; p++) {
                partOffsets[p] = offset;
                offset += parts[p].length + 1;
              }
            }
            const restPathString = path.slice(partOffsets[i]);
            const m = matcher.exec(restPathString);
            if (m) {
              params[name] = m[0];
              this.#pushHandlerSets(handlerSets, child, method, node.#params, params);
              if (m[0].length === restPathString.length && child.#children["*"]) {
                this.#pushHandlerSets(
                  handlerSets,
                  child.#children["*"],
                  method,
                  node.#params,
                  params
                );
              }
              for (const _ in child.#children) {
                child.#params = params;
                const componentCount = m[0].match(/\//g)?.length ?? 0;
                const targetCurNodes = curNodesQueue[componentCount] ||= [];
                targetCurNodes.push(child);
                break;
              }
              continue;
            }
          }
          if (matcher === true || matcher.test(part)) {
            params[name] = part;
            if (isLast) {
              this.#pushHandlerSets(handlerSets, child, method, params, node.#params);
              if (child.#children["*"]) {
                this.#pushHandlerSets(
                  handlerSets,
                  child.#children["*"],
                  method,
                  params,
                  node.#params
                );
              }
            } else {
              child.#params = params;
              tempNodes.push(child);
            }
          }
        }
      }
      const shifted = curNodesQueue.shift();
      curNodes = shifted ? tempNodes.concat(shifted) : tempNodes;
    }
    if (handlerSets[1]) {
      handlerSets.sort((a, b) => {
        return a.score - b.score;
      });
    }
    return [handlerSets.map(({ handler, params }) => [handler, params])];
  }
};

// ../../../../gacha-audit-20260907/server/node_modules/hono/dist/router/trie-router/router.js
var TrieRouter = class {
  static {
    __name(this, "TrieRouter");
  }
  name = "TrieRouter";
  #node = new Node2();
  add(method, path, handler) {
    for (const result of checkOptionalParameter(path) || [path]) {
      this.#node.insert(method, result, handler);
    }
  }
  match(method, path) {
    return this.#node.search(method, path);
  }
};

// ../../../../gacha-audit-20260907/server/node_modules/hono/dist/hono.js
var Hono2 = class extends Hono {
  static {
    __name(this, "Hono");
  }
  /**
   * Creates an instance of the Hono class.
   *
   * @param options - Optional configuration options for the Hono instance.
   */
  constructor(options = {}) {
    super(options);
    this.router = options.router ?? new SmartRouter({
      routers: [new RegExpRouter(), new TrieRouter()]
    });
  }
};

// ../../packages/common/src/canonical-json.ts
function normalize(value) {
  if (Array.isArray(value)) {
    return value.map((item) => normalize(item));
  }
  if (value !== null && typeof value === "object") {
    return Object.keys(value).sort().reduce((acc, key) => {
      acc[key] = normalize(value[key] ?? null);
      return acc;
    }, {});
  }
  return value;
}
__name(normalize, "normalize");
function canonicalJson(value) {
  return JSON.stringify(normalize(value));
}
__name(canonicalJson, "canonicalJson");
async function sha256Hex(input) {
  const digest = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(input));
  return [...new Uint8Array(digest)].map((byte) => byte.toString(16).padStart(2, "0")).join("");
}
__name(sha256Hex, "sha256Hex");

// ../../packages/common/src/request-id.ts
var ENCODING = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
function encodeCharacter(index) {
  const character = ENCODING[index];
  if (character === void 0) {
    throw new Error("ULID_ENCODING_INDEX_OUT_OF_RANGE");
  }
  return character;
}
__name(encodeCharacter, "encodeCharacter");
function encodeTime(time) {
  let value = time;
  let output = "";
  for (let index = 0; index < 10; index += 1) {
    output = encodeCharacter(value % 32) + output;
    value = Math.floor(value / 32);
  }
  return output;
}
__name(encodeTime, "encodeTime");
function encodeRandom() {
  const bytes = new Uint8Array(16);
  crypto.getRandomValues(bytes);
  let output = "";
  for (const byte of bytes) {
    output += encodeCharacter(byte % 32);
  }
  return output;
}
__name(encodeRandom, "encodeRandom");
function createRequestId() {
  return `req_${encodeTime(Date.now())}${encodeRandom()}`;
}
__name(createRequestId, "createRequestId");

// ../../packages/common/src/server-release-policy.ts
var serviceValues = /* @__PURE__ */ new Set(["game-api", "admin-api", "async-worker"]);
var environmentValues = /* @__PURE__ */ new Set(["dev", "prd"]);
var dbCompatibilityValues = /* @__PURE__ */ new Set(["NO_DB_CHANGE", "ADDITIVE_COMPATIBLE", "MIN_VERSION_GATE_REQUIRED"]);
var apiCompatibilityValues = /* @__PURE__ */ new Set(["NO_API_CHANGE", "ROLLBACK_COMPATIBLE", "MIN_VERSION_GATE_REQUIRED"]);
var verificationValues = /* @__PURE__ */ new Set(["PENDING", "PASSED", "FAILED", "BLOCKED"]);
var safeIdPattern = /^[A-Za-z0-9][A-Za-z0-9._:-]{0,127}$/u;
var safeBranchPattern = /^release\/[A-Za-z0-9][A-Za-z0-9._/-]{0,160}$/u;
var safeTagPattern = /^[A-Za-z0-9][A-Za-z0-9._:-]{0,160}$/u;
var commitShaPattern = /^[a-f0-9]{7,40}$/u;
var versionCodePattern = /^[1-9]\d{0,9}$/u;
var serverVersionPattern = /^(20\d{2})\.(0[1-9]|1[0-2])\.(0[1-9]|[12]\d|3[01])\.([1-9]\d{0,3})$/u;
function isRecord(value) {
  return Boolean(value) && typeof value === "object" && !Array.isArray(value);
}
__name(isRecord, "isRecord");
function isVersionCode(value) {
  return typeof value === "string" && versionCodePattern.test(value) && Number(value) <= 2147483647;
}
__name(isVersionCode, "isVersionCode");
function isSafeId(value) {
  return typeof value === "string" && safeIdPattern.test(value);
}
__name(isSafeId, "isSafeId");
function isServerReleaseService(value) {
  return typeof value === "string" && serviceValues.has(value);
}
__name(isServerReleaseService, "isServerReleaseService");
function isServerReleaseEnvironment(value) {
  return typeof value === "string" && environmentValues.has(value);
}
__name(isServerReleaseEnvironment, "isServerReleaseEnvironment");
function isCalendarServerVersion(value) {
  if (typeof value !== "string") return false;
  const match2 = serverVersionPattern.exec(value);
  if (!match2) return false;
  const year = Number(match2[1]);
  const month = Number(match2[2]);
  const day = Number(match2[3]);
  const date = new Date(Date.UTC(year, month - 1, day));
  return date.getUTCFullYear() === year && date.getUTCMonth() === month - 1 && date.getUTCDate() === day;
}
__name(isCalendarServerVersion, "isCalendarServerVersion");
function parseServices(value) {
  if (!Array.isArray(value) || value.length === 0) return null;
  const services = [];
  for (const service of value) {
    if (!isServerReleaseService(service) || services.includes(service)) return null;
    services.push(service);
  }
  return services;
}
__name(parseServices, "parseServices");
function parseDeploymentTags(value) {
  if (!Array.isArray(value) || value.length === 0) return null;
  const tags = [];
  for (const item of value) {
    if (!isRecord(item)) return null;
    if (!isServerReleaseService(item.service)) return null;
    if (typeof item.tag !== "string" || !safeTagPattern.test(item.tag)) return null;
    if (tags.some((existing) => existing.service === item.service)) return null;
    tags.push({ service: item.service, tag: item.tag });
  }
  return tags;
}
__name(parseDeploymentTags, "parseDeploymentTags");
function parseServerDeployPlan(value) {
  if (!isRecord(value)) return null;
  if (!isSafeId(value.releaseId)) return null;
  if (!isServerReleaseEnvironment(value.environment)) return null;
  const services = parseServices(value.services);
  if (services === null) return null;
  if (!isCalendarServerVersion(value.serverVersion)) return null;
  if (!isRecord(value.source)) return null;
  if (typeof value.source.commitSha !== "string" || !commitShaPattern.test(value.source.commitSha)) return null;
  if (typeof value.source.releaseBranch !== "string" || !safeBranchPattern.test(value.source.releaseBranch)) return null;
  if (value.source.workingTreeClean !== true) return null;
  const deploymentTags = parseDeploymentTags(value.deploymentTags);
  if (deploymentTags === null) return null;
  if (!isRecord(value.database) || typeof value.database.compatibility !== "string") return null;
  const compatibility = value.database.compatibility;
  if (!dbCompatibilityValues.has(compatibility)) return null;
  if (value.database.minimumVersionCode !== void 0 && !isVersionCode(value.database.minimumVersionCode)) return null;
  if (value.database.migrationRange !== void 0 && !isSafeId(value.database.migrationRange)) return null;
  let api;
  if (value.api !== void 0) {
    if (!isRecord(value.api) || typeof value.api.compatibility !== "string") return null;
    const apiCompatibility = value.api.compatibility;
    if (!apiCompatibilityValues.has(apiCompatibility)) return null;
    if (value.api.rollbackVerified !== void 0 && typeof value.api.rollbackVerified !== "boolean") return null;
    if (value.api.minimumVersionCode !== void 0 && !isVersionCode(value.api.minimumVersionCode)) return null;
    api = {
      compatibility: apiCompatibility,
      ...value.api.rollbackVerified === void 0 ? {} : { rollbackVerified: value.api.rollbackVerified },
      ...value.api.minimumVersionCode === void 0 ? {} : { minimumVersionCode: value.api.minimumVersionCode }
    };
  }
  if (!isRecord(value.verification)) return null;
  if (value.verification.localRegression !== "PASSED" || value.verification.dryRun !== "PASSED") return null;
  if (typeof value.verification.remoteSmoke !== "string" || !verificationValues.has(value.verification.remoteSmoke)) return null;
  let contentRelease;
  if (value.contentRelease !== void 0) {
    if (!isRecord(value.contentRelease)) return null;
    if (!isSafeId(value.contentRelease.releaseId) || !isSafeId(value.contentRelease.contentVersion)) return null;
    contentRelease = {
      releaseId: value.contentRelease.releaseId,
      contentVersion: value.contentRelease.contentVersion
    };
  }
  return {
    releaseId: value.releaseId,
    environment: value.environment,
    services,
    serverVersion: value.serverVersion,
    source: {
      commitSha: value.source.commitSha,
      releaseBranch: value.source.releaseBranch,
      workingTreeClean: true
    },
    deploymentTags,
    database: {
      compatibility,
      ...value.database.minimumVersionCode === void 0 ? {} : { minimumVersionCode: value.database.minimumVersionCode },
      ...value.database.migrationRange === void 0 ? {} : { migrationRange: value.database.migrationRange }
    },
    ...api === void 0 ? {} : { api },
    ...contentRelease === void 0 ? {} : { contentRelease },
    verification: {
      localRegression: "PASSED",
      dryRun: "PASSED",
      remoteSmoke: value.verification.remoteSmoke
    }
  };
}
__name(parseServerDeployPlan, "parseServerDeployPlan");

// ../../packages/common/src/server-deployment-history.ts
var safeDeploymentIdPattern = /^[A-Za-z0-9][A-Za-z0-9._:-]{0,160}$/u;
function isRecord2(value) {
  return Boolean(value) && typeof value === "object" && !Array.isArray(value);
}
__name(isRecord2, "isRecord");
function buildServerDeploymentHistoryKey(environment, service, serverVersion) {
  return `server-deployments/${environment}/${service}/${serverVersion}.json`;
}
__name(buildServerDeploymentHistoryKey, "buildServerDeploymentHistoryKey");
function parseServerDeploymentEvidence(value) {
  if (!isRecord2(value)) return null;
  const plan = parseServerDeployPlan({
    releaseId: value.releaseId,
    environment: value.environment,
    services: [value.service],
    serverVersion: value.serverVersion,
    source: {
      commitSha: value.commitSha,
      releaseBranch: value.releaseBranch,
      workingTreeClean: true
    },
    deploymentTags: [{ service: value.service, tag: value.deploymentTag }],
    database: value.database,
    ...value.api === void 0 ? {} : { api: value.api },
    ...value.contentRelease === null ? {} : { contentRelease: value.contentRelease },
    verification: value.verification
  });
  if (plan === null) return null;
  if (typeof value.cloudflareDeploymentId !== "string" || !safeDeploymentIdPattern.test(value.cloudflareDeploymentId)) return null;
  if (value.previousDeploymentId !== null && (typeof value.previousDeploymentId !== "string" || !safeDeploymentIdPattern.test(value.previousDeploymentId))) return null;
  if (value.pointerStatus !== "CURRENT" && value.pointerStatus !== "DEGRADED") return null;
  if (typeof value.createdAt !== "string" || Number.isNaN(Date.parse(value.createdAt))) return null;
  const service = plan.services[0];
  const deploymentTag = plan.deploymentTags[0];
  if (service === void 0 || deploymentTag === void 0) return null;
  return {
    releaseId: plan.releaseId,
    environment: plan.environment,
    service,
    serverVersion: plan.serverVersion,
    commitSha: plan.source.commitSha,
    releaseBranch: plan.source.releaseBranch,
    deploymentTag: deploymentTag.tag,
    cloudflareDeploymentId: value.cloudflareDeploymentId,
    previousDeploymentId: value.previousDeploymentId,
    database: plan.database,
    ...plan.api === void 0 ? {} : { api: plan.api },
    contentRelease: plan.contentRelease ?? null,
    verification: plan.verification,
    pointerStatus: value.pointerStatus,
    createdAt: value.createdAt
  };
}
__name(parseServerDeploymentEvidence, "parseServerDeploymentEvidence");
function parseServerDeploymentCurrentPointer(value) {
  if (!isRecord2(value)) return null;
  if (value.environment !== "dev" && value.environment !== "prd" || value.service !== "game-api" && value.service !== "admin-api" && value.service !== "async-worker" || typeof value.serverVersion !== "string" || typeof value.historyKey !== "string" || typeof value.releaseId !== "string" || typeof value.cloudflareDeploymentId !== "string" || typeof value.updatedAt !== "string") {
    return null;
  }
  return {
    environment: value.environment,
    service: value.service,
    serverVersion: value.serverVersion,
    historyKey: value.historyKey,
    releaseId: value.releaseId,
    cloudflareDeploymentId: value.cloudflareDeploymentId,
    updatedAt: value.updatedAt
  };
}
__name(parseServerDeploymentCurrentPointer, "parseServerDeploymentCurrentPointer");

// ../../packages/common/src/version-policy.ts
function clientReleaseRegistrySupportsEnvironment(registry, environment) {
  if (!registry) return true;
  if (environment === "PRD") return true;
  return (environment === "DEV" || environment === "LOCAL") && registry.entries.every((entry) => entry.status !== "INTERNAL");
}
__name(clientReleaseRegistrySupportsEnvironment, "clientReleaseRegistrySupportsEnvironment");
var currentVersionKey = "versions/current.json";
var safeIdPattern2 = /^[A-Za-z0-9][A-Za-z0-9._:-]{0,127}$/u;
var safeVersionPattern = /^[A-Za-z0-9][A-Za-z0-9._-]{0,99}$/u;
var sha256Pattern = /^sha256:[a-f0-9]{64}$/u;
function isVersionCode2(value) {
  return typeof value === "string" && /^[1-9]\d{0,9}$/u.test(value) && Number(value) <= 2147483647;
}
__name(isVersionCode2, "isVersionCode");
function parseContentRelease(value) {
  if (!value || typeof value !== "object" || Array.isArray(value)) return null;
  const raw2 = value;
  if (raw2.status !== "APPROVED") return null;
  if (typeof raw2.releaseId !== "string" || !safeIdPattern2.test(raw2.releaseId)) return null;
  if (typeof raw2.contentVersion !== "string" || !safeIdPattern2.test(raw2.contentVersion)) return null;
  if (typeof raw2.catalogUrl !== "string") return null;
  let catalogUrl;
  try {
    catalogUrl = new URL(raw2.catalogUrl);
  } catch {
    return null;
  }
  if (catalogUrl.protocol !== "https:") return null;
  if (typeof raw2.catalogHash !== "string" || !sha256Pattern.test(raw2.catalogHash)) return null;
  if (!isVersionCode2(raw2.minimumVersionCode)) return null;
  if (raw2.maximumVersionCode !== void 0 && !isVersionCode2(raw2.maximumVersionCode)) return null;
  if (raw2.maximumVersionCode !== void 0 && Number(raw2.maximumVersionCode) < Number(raw2.minimumVersionCode)) return null;
  return {
    status: "APPROVED",
    releaseId: raw2.releaseId,
    contentVersion: raw2.contentVersion,
    catalogUrl: raw2.catalogUrl,
    catalogHash: raw2.catalogHash,
    minimumVersionCode: raw2.minimumVersionCode,
    ...raw2.maximumVersionCode === void 0 ? {} : { maximumVersionCode: raw2.maximumVersionCode }
  };
}
__name(parseContentRelease, "parseContentRelease");
function parseClientReleaseRegistry(value) {
  if (!value || typeof value !== "object" || Array.isArray(value)) return null;
  const raw2 = value;
  if (Object.keys(raw2).some((key) => !["schemaVersion", "entries", "auditHead"].includes(key))) return null;
  if (raw2.schemaVersion !== 1 || !Array.isArray(raw2.entries) || raw2.entries.length > 1e3) return null;
  if (raw2.auditHead !== void 0 && (typeof raw2.auditHead !== "string" || !safeIdPattern2.test(raw2.auditHead))) return null;
  const entries = [];
  const codes = /* @__PURE__ */ new Set();
  const releaseIdentities = /* @__PURE__ */ new Map();
  for (const value2 of raw2.entries) {
    if (!value2 || typeof value2 !== "object" || Array.isArray(value2)) return null;
    const entry = value2;
    if (Object.keys(entry).some((key) => !["version", "versionCode", "status", "contentRelease"].includes(key))) return null;
    if (typeof entry.version !== "string" || !safeVersionPattern.test(entry.version) || !isVersionCode2(entry.versionCode) || codes.has(entry.versionCode)) return null;
    if (entry.status !== "INTERNAL" && entry.status !== "STABLE" && entry.status !== "DISABLED") return null;
    const contentRelease = parseContentRelease(entry.contentRelease);
    if (!contentRelease || Number(entry.versionCode) < Number(contentRelease.minimumVersionCode) || contentRelease.maximumVersionCode !== void 0 && Number(entry.versionCode) > Number(contentRelease.maximumVersionCode)) return null;
    const url = new URL(contentRelease.catalogUrl);
    if (url.username || url.password || url.search || url.hash) return null;
    const identity = JSON.stringify(contentRelease);
    const previousIdentity = releaseIdentities.get(contentRelease.releaseId);
    if (previousIdentity !== void 0 && previousIdentity !== identity) return null;
    releaseIdentities.set(contentRelease.releaseId, identity);
    codes.add(entry.versionCode);
    entries.push({ version: entry.version, versionCode: entry.versionCode, status: entry.status, contentRelease });
  }
  return { schemaVersion: 1, entries, ...raw2.auditHead === void 0 ? {} : { auditHead: raw2.auditHead } };
}
__name(parseClientReleaseRegistry, "parseClientReleaseRegistry");
function parseVersion(value) {
  if (!value || typeof value !== "object" || Array.isArray(value)) return null;
  const raw2 = value;
  if (typeof raw2.version !== "string" || !safeVersionPattern.test(raw2.version)) return null;
  if (!isVersionCode2(raw2.versionCode)) return null;
  if (raw2.minimumVersionCode !== void 0) {
    if (!isVersionCode2(raw2.minimumVersionCode) || Number(raw2.minimumVersionCode) > Number(raw2.versionCode)) return null;
  }
  const registry = raw2.clientReleaseRegistry === void 0 ? void 0 : parseClientReleaseRegistry(raw2.clientReleaseRegistry);
  if (registry === null || registry !== void 0 && raw2.minimumVersionCode === void 0) return null;
  if (raw2.contentRelease !== void 0) {
    const contentRelease = parseContentRelease(raw2.contentRelease);
    if (contentRelease === null) return null;
    return {
      version: raw2.version,
      versionCode: raw2.versionCode,
      ...raw2.minimumVersionCode === void 0 ? {} : { minimumVersionCode: raw2.minimumVersionCode },
      contentRelease,
      ...registry === void 0 ? {} : { clientReleaseRegistry: registry }
    };
  }
  return {
    version: raw2.version,
    versionCode: raw2.versionCode,
    ...raw2.minimumVersionCode === void 0 ? {} : { minimumVersionCode: raw2.minimumVersionCode },
    ...registry === void 0 ? {} : { clientReleaseRegistry: registry }
  };
}
__name(parseVersion, "parseVersion");
async function readVersionSnapshot(bucket) {
  const object = await bucket.get(currentVersionKey);
  if (object === null) return { version: null, etag: null };
  const version = parseVersion(await object.json());
  if (version === null) throw new Error("version_document_invalid");
  const etag = object.httpEtag ?? (object.etag ? `"${object.etag}"` : null);
  if (!etag) throw new Error("version_etag_unavailable");
  return { version, etag };
}
__name(readVersionSnapshot, "readVersionSnapshot");
async function saveVersion(bucket, version, options = {}) {
  const expectedEtag = options.expectedEtag === void 0 ? (await readVersionSnapshot(bucket)).etag : options.expectedEtag;
  if (expectedEtag === "") throw new Error("version_etag_unavailable");
  const onlyIf = new Headers();
  if (expectedEtag === null) onlyIf.set("If-None-Match", "*");
  else onlyIf.set("If-Match", expectedEtag);
  const result = await bucket.put(currentVersionKey, JSON.stringify(version), {
    onlyIf,
    httpMetadata: { contentType: "application/json", cacheControl: "no-store" }
  });
  if (result === null) throw new Error("version_current_pointer_conflict");
  if (result === void 0) throw new Error("version_storage_write_failed");
  return version;
}
__name(saveVersion, "saveVersion");

// src/ops-log-search.ts
function opsLogSearch(category, search, serverVersion, appVersion, appVersionCode) {
  if (category && category !== "server" && category !== "frontend") throw new Error("invalid_log_category");
  const q = search?.trim() ?? "";
  if (q.length > 200) throw new Error("search_too_long");
  const clauses = [];
  const values = [];
  const version = serverVersion?.trim();
  if (version) {
    if (!/^[A-Za-z0-9._-]{1,100}$/.test(version)) throw new Error("invalid_server_version");
    clauses.push("json_extract(CASE WHEN json_valid(sanitized_context_json) THEN sanitized_context_json ELSE '{}' END, '$.serverVersion') = ?");
    values.push(version);
  }
  const frontendAppVersion = appVersion?.trim();
  if (frontendAppVersion) {
    if (frontendAppVersion.length > 64) throw new Error("invalid_app_version");
    clauses.push("json_extract(CASE WHEN json_valid(sanitized_context_json) THEN sanitized_context_json ELSE '{}' END, '$.appVersion') = ?");
    values.push(frontendAppVersion);
  }
  const frontendAppVersionCode = appVersionCode?.trim();
  if (frontendAppVersionCode) {
    if (!/^\d{1,19}$/.test(frontendAppVersionCode)) throw new Error("invalid_app_version_code");
    clauses.push("json_extract(CASE WHEN json_valid(sanitized_context_json) THEN sanitized_context_json ELSE '{}' END, '$.appVersionCode') = ?");
    values.push(frontendAppVersionCode);
  }
  if (category === "frontend") {
    clauses.push("json_extract(CASE WHEN json_valid(sanitized_context_json) THEN sanitized_context_json ELSE '{}' END, '$.clientDiagnosticCategory') = 'frontend'");
  }
  if (category === "server") {
    clauses.push("COALESCE(json_extract(CASE WHEN json_valid(sanitized_context_json) THEN sanitized_context_json ELSE '{}' END, '$.clientDiagnosticCategory'), '') != 'frontend'");
  }
  if (q) {
    const fields2 = ["id", "error_code", "message_summary", "request_id", "service", "source", "account_id", "run_id", "trace_ref", "correlation_json"];
    clauses.push(`(${fields2.map((field) => `instr(lower(COALESCE(${field}, '')), lower(?)) > 0`).join(" OR ")})`);
    values.push(...fields2.map(() => q));
  }
  return { clauses, values };
}
__name(opsLogSearch, "opsLogSearch");

// src/client-release-registry.ts
var historyPrefix = "versions/client-release-history/";
var fail = /* @__PURE__ */ __name((code) => {
  throw new Error(code);
}, "fail");
var fields = {
  register: ["version", "versionCode", "contentRelease", "legacyTransitionConfirmation"],
  status: ["status"],
  promote: ["confirmation", "expectedArtifactIdentity"],
  minimum: ["minimumVersionCode", "confirmMinimumChange"],
  rollback: ["historyId", "confirmation"]
};
async function artifactIdentity(entry) {
  return `sha256:${await sha256Hex(canonicalJson(entry.contentRelease))}`;
}
__name(artifactIdentity, "artifactIdentity");
async function describe(version, revision, target, canMutate) {
  if (!clientReleaseRegistrySupportsEnvironment(version?.clientReleaseRegistry, target)) return fail("registry_document_environment_invalid");
  const production = target === "PRD";
  return {
    target,
    registryStatus: version?.clientReleaseRegistry ? "CONFIGURED" : "LEGACY",
    revision,
    minimumVersionCode: version?.minimumVersionCode ?? null,
    auditHead: version?.clientReleaseRegistry?.auditHead ?? null,
    rows: await Promise.all((version?.clientReleaseRegistry?.entries ?? []).map(async (entry) => ({
      ...entry,
      artifactIdentity: await artifactIdentity(entry),
      validation: { status: "STRUCTURAL_ONLY", artifactBytesVerified: false }
    }))),
    distributionPolicy: {
      supportedStatuses: production ? ["INTERNAL", "STABLE", "DISABLED"] : ["STABLE", "DISABLED"],
      initialStatus: production ? "INTERNAL" : "STABLE",
      mutableStatuses: production ? ["INTERNAL", "DISABLED"] : ["STABLE", "DISABLED"],
      testerGate: production
    },
    canMutate,
    capabilities: { register: canMutate, changeStatus: canMutate, promote: canMutate && production, changeMinimum: canMutate, rollback: canMutate, manageTesters: false }
  };
}
__name(describe, "describe");
async function describeClientReleaseRegistry(bucket, target, canMutate) {
  const snapshot = await readVersionSnapshot(bucket);
  return describe(snapshot.version, snapshot.etag, target, canMutate);
}
__name(describeClientReleaseRegistry, "describeClientReleaseRegistry");
async function committedHistory(bucket, current, id, target) {
  if (!/^[A-Za-z0-9-]{1,128}$/u.test(id)) return fail("registry_history_invalid");
  let head = current.clientReleaseRegistry?.auditHead;
  const visited = /* @__PURE__ */ new Set();
  for (let count = 0; head && count < 1e3; count++) {
    if (visited.has(head)) break;
    visited.add(head);
    const object = await bucket.get(`${historyPrefix}${head}.json`);
    const row = object ? await object.json() : null;
    if (!row || row.schemaVersion !== 1 || row.id !== head || row.target !== target || !parseVersion(row.before) || !parseVersion(row.after)) return fail("registry_history_invalid");
    if (head === id) return row;
    head = row.parent ?? void 0;
  }
  return fail("registry_history_not_committed");
}
__name(committedHistory, "committedHistory");
async function mutateClientReleaseRegistry(bucket, target, actorId, operation, body, versionCode) {
  if (target !== "DEV" && target !== "PRD" || body.target !== target) return fail("registry_target_mismatch");
  const allowed = fields[operation];
  if (!allowed || Object.keys(body).some((key) => !["target", "expectedRevision", "reason", ...allowed].includes(key))) return fail("registry_body_invalid");
  if (!("expectedRevision" in body)) return fail("version_precondition_required");
  if (typeof body.expectedRevision !== "string" || !/^"[\x21\x23-\x7e]{1,256}"$/u.test(body.expectedRevision)) return fail("invalid_version_revision");
  if (typeof body.reason !== "string" || !body.reason.trim() || body.reason.length > 500) return fail("registry_reason_required");
  const snapshot = await readVersionSnapshot(bucket);
  if (snapshot.etag !== body.expectedRevision) return fail("version_current_pointer_conflict");
  const before = snapshot.version;
  if (!before?.minimumVersionCode) return fail("version_policy_unconfigured");
  if (!clientReleaseRegistrySupportsEnvironment(before.clientReleaseRegistry, target)) return fail("registry_document_environment_invalid");
  if (operation === "promote" && target !== "PRD") return fail("registry_promotion_environment_disabled");
  let after = structuredClone(before);
  const entries = after.clientReleaseRegistry?.entries ?? [];
  after.clientReleaseRegistry = { schemaVersion: 1, entries };
  const entry = entries.find((row) => row.versionCode === versionCode);
  if (operation === "register") {
    if (!before.clientReleaseRegistry && body.legacyTransitionConfirmation !== "ENABLE_EXACT_LOOKUP") return fail("registry_transition_confirmation_required");
    if (entries.some((row) => row.versionCode === body.versionCode)) return fail("version_code_already_registered");
    const registered = { version: body.version, versionCode: body.versionCode, status: target === "DEV" ? "STABLE" : "INTERNAL", contentRelease: body.contentRelease };
    entries.push(registered);
    if (target === "DEV" && Number(registered.versionCode) >= Number(after.versionCode)) {
      after.version = registered.version;
      after.versionCode = registered.versionCode;
    }
  } else if (operation === "status" || operation === "promote") {
    if (!entry) return fail("client_version_unregistered");
    if (operation === "status") {
      if (body.status !== (target === "DEV" ? "STABLE" : "INTERNAL") && body.status !== "DISABLED") return fail("registry_status_invalid");
      entry.status = body.status === "DISABLED" ? "DISABLED" : target === "DEV" ? "STABLE" : "INTERNAL";
      if (target === "DEV" && entry.status === "STABLE" && Number(entry.versionCode) >= Number(after.versionCode)) {
        after.version = entry.version;
        after.versionCode = entry.versionCode;
      }
    } else {
      if (entry.status !== "INTERNAL") return fail("registry_promotion_state_invalid");
      if (body.confirmation !== "PROMOTE_UNCHANGED_ARTIFACT" || body.expectedArtifactIdentity !== await artifactIdentity(entry)) return fail("registry_artifact_identity_conflict");
      entry.status = "STABLE";
      if (Number(entry.versionCode) >= Number(after.versionCode)) {
        after.version = entry.version;
        after.versionCode = entry.versionCode;
      }
    }
  } else if (operation === "minimum") {
    if (body.confirmMinimumChange !== true || !isVersionCode2(body.minimumVersionCode)) return fail("registry_minimum_confirmation_required");
    if (!entries.some((row) => row.status === "STABLE" && Number(row.versionCode) >= Number(body.minimumVersionCode))) return fail("registry_minimum_without_stable");
    after.minimumVersionCode = body.minimumVersionCode;
  } else {
    if (body.confirmation !== "RESTORE_REGISTRY" || typeof body.historyId !== "string") return fail("registry_rollback_confirmation_required");
    const history2 = await committedHistory(bucket, before, body.historyId, target);
    if (!history2.before.clientReleaseRegistry) return fail("registry_legacy_rollback_forbidden");
    after = { ...structuredClone(history2.before), minimumVersionCode: before.minimumVersionCode };
    const restoredEntries = after.clientReleaseRegistry.entries;
    for (const registered of before.clientReleaseRegistry?.entries ?? []) {
      if (!restoredEntries.some((row) => row.versionCode === registered.versionCode)) {
        restoredEntries.push({ ...structuredClone(registered), status: "DISABLED" });
      }
    }
    if (!after.clientReleaseRegistry?.entries.some((row) => row.status === "STABLE" && Number(row.versionCode) >= Number(before.minimumVersionCode))) return fail("registry_minimum_without_stable");
  }
  const parsed = parseVersion(after);
  if (!parsed?.clientReleaseRegistry) return fail("registry_document_invalid");
  if (!clientReleaseRegistrySupportsEnvironment(parsed.clientReleaseRegistry, target)) return fail("registry_document_environment_invalid");
  const id = crypto.randomUUID();
  parsed.clientReleaseRegistry.auditHead = id;
  const history = {
    schemaVersion: 1,
    id,
    parent: before.clientReleaseRegistry?.auditHead ?? null,
    target,
    actorId,
    reason: body.reason.trim(),
    operation,
    createdAt: (/* @__PURE__ */ new Date()).toISOString(),
    expectedRevision: body.expectedRevision,
    before,
    after: parsed
  };
  const onlyIf = new Headers({ "If-None-Match": "*" });
  const written = await bucket.put(`${historyPrefix}${id}.json`, JSON.stringify(history), { onlyIf, httpMetadata: { contentType: "application/json", cacheControl: "no-store" } });
  if (!written) return fail("registry_history_write_failed");
  await saveVersion(bucket, parsed, { expectedEtag: snapshot.etag });
  return describeClientReleaseRegistry(bucket, target, true);
}
__name(mutateClientReleaseRegistry, "mutateClientReleaseRegistry");

// src/index.ts
var app = new Hono2();
var BALANCE_PUBLISH_PERMISSION = "BALANCE_PUBLISH";
var ACCOUNT_NOTE_WRITE_PERMISSION = "ACCOUNT_NOTE_WRITE";
var STORY_PROGRESS_READ_PERMISSION = "STORY_PROGRESS_READ";
var OFFLINE_REWARD_READ_PERMISSION = "OFFLINE_REWARD_READ";
var SUPPORT_READ_PERMISSION = "SUPPORT_READ";
var OPS_ERROR_LOG_READ_PERMISSION = "OPS_ERROR_LOG_READ";
var SERVER_DEPLOYMENT_READ_PERMISSION = "SERVER_DEPLOYMENT_READ";
var LAUNCH_LOGIN_BONUS_ID = "launch_login_bonus_act1_v1";
var LAUNCH_LOGIN_BONUS_EXPECTED_REWARD_LINES = [
  { kind: "ITEM", id: "cur_command_ticket", quantity: 5, source: "LAUNCH_LOGIN_BONUS" }
];
var CONFIRMATION_TTL_MS = 5 * 60 * 1e3;
var ACT_ONE_NARRATIVE_VERSION = "act1.2026-08-27.001";
var ACT_ONE_NARRATIVE_COVERAGE_VERSION = "act1.2026-08-30.001";
var ACT_ONE_STORY_PROGRESS_ORDER = [
  "none",
  "opn_01_last_departure",
  "opn_02_first_signal",
  "opn_03_water_stop",
  "opn_04_scrapstar_show",
  "opn_05_night_boarders",
  "opn_06_broken_line",
  "raid_raider_king_engine"
];
var ACT_ONE_OPERATION_IDS = ACT_ONE_STORY_PROGRESS_ORDER.filter((id) => id !== "none");
var ACT_ONE_NARRATIVE_COVERAGE = [
  { missionId: "opn_01_last_departure", displayName: "\uB9C8\uC9C0\uB9C9 \uCD9C\uBC1C" },
  { missionId: "opn_02_first_signal", displayName: "\uCCAB \uC2E0\uD638" },
  { missionId: "opn_03_water_stop", displayName: "\uAE09\uC218 \uC815\uCC28" },
  { missionId: "opn_04_scrapstar_show", displayName: "\uC2A4\uD06C\uB7A9\uC2A4\uD0C0 \uC1FC" },
  { missionId: "opn_05_night_boarders", displayName: "\uC57C\uAC04 \uC2B9\uC120\uC804" },
  { missionId: "opn_06_broken_line", displayName: "\uB04A\uC5B4\uC9C4 \uC120\uB85C" },
  { missionId: "raid_raider_king_engine", displayName: "\uC57D\uD0C8\uC655 \uAE30\uAD00\uCC28" }
];
function decodeBase64Url(value) {
  const padded = value.replaceAll("-", "+").replaceAll("_", "/").padEnd(Math.ceil(value.length / 4) * 4, "=");
  const binary = atob(padded);
  return Uint8Array.from(binary, (char) => char.charCodeAt(0));
}
__name(decodeBase64Url, "decodeBase64Url");
function decodeJson(value) {
  return JSON.parse(new TextDecoder().decode(decodeBase64Url(value)));
}
__name(decodeJson, "decodeJson");
function hasAudience(payloadAudience, expectedAudience) {
  if (typeof payloadAudience === "string") return payloadAudience === expectedAudience;
  if (Array.isArray(payloadAudience)) return payloadAudience.includes(expectedAudience);
  return false;
}
__name(hasAudience, "hasAudience");
async function verifyAccessJwt(token, issuer, audience, options = {}) {
  const nowSeconds = options.nowSeconds ?? Math.floor(Date.now() / 1e3);
  const [encodedHeader, encodedPayload, encodedSignature] = token.split(".");
  if (encodedHeader === void 0 || encodedPayload === void 0 || encodedSignature === void 0) return null;
  const header = decodeJson(encodedHeader);
  if (header.alg !== "RS256" || typeof header.kid !== "string") return null;
  const jwksResponse = await fetch(`${issuer}/cdn-cgi/access/certs`);
  if (!jwksResponse.ok) return null;
  const jwks = await jwksResponse.json();
  const jwk = jwks.keys?.find((key) => key.kid === header.kid && key.kty === "RSA");
  if (jwk === void 0) return null;
  const publicKey = await crypto.subtle.importKey(
    "jwk",
    jwk,
    { name: "RSASSA-PKCS1-v1_5", hash: "SHA-256" },
    false,
    ["verify"]
  );
  const signingInput = `${encodedHeader}.${encodedPayload}`;
  const verified = await crypto.subtle.verify(
    "RSASSA-PKCS1-v1_5",
    publicKey,
    decodeBase64Url(encodedSignature),
    new TextEncoder().encode(signingInput)
  );
  if (!verified) return null;
  const payload = decodeJson(encodedPayload);
  if (payload.iss !== issuer) return null;
  if (!hasAudience(payload.aud, audience)) return null;
  if (typeof payload.exp !== "number" || payload.exp <= nowSeconds) return null;
  if (typeof payload.nbf === "number" && payload.nbf > nowSeconds) return null;
  if (typeof payload.sub === "string" && payload.sub.length > 0) {
    return {
      kind: "user",
      subject: payload.sub,
      email: typeof payload.email === "string" && payload.email.length > 0 ? payload.email : null
    };
  }
  if (options.allowServiceToken && typeof payload.common_name === "string" && payload.common_name.length > 0) {
    return {
      kind: "service",
      commonName: payload.common_name
    };
  }
  return null;
}
__name(verifyAccessJwt, "verifyAccessJwt");
async function extractDelegatedAccessIdentity(c) {
  if (c.env.ADMIN_DELEGATED_ACCESS_ISSUER.length === 0 || c.env.ADMIN_DELEGATED_ACCESS_AUD.length === 0) {
    return c.json({ error: "ADMIN_DELEGATED_ACCESS_JWT_VERIFICATION_NOT_CONFIGURED" }, 501);
  }
  const delegatedToken = c.req.header("X-Lastrail-User-Access-Jwt");
  if (!delegatedToken) {
    return c.json({ error: "ADMIN_DELEGATED_ACCESS_JWT_REQUIRED" }, 401);
  }
  const delegatedIdentity = await verifyAccessJwt(
    delegatedToken,
    c.env.ADMIN_DELEGATED_ACCESS_ISSUER,
    c.env.ADMIN_DELEGATED_ACCESS_AUD
  );
  if (delegatedIdentity === null || delegatedIdentity.kind !== "user") {
    return c.json({ error: "ADMIN_DELEGATED_ACCESS_JWT_INVALID" }, 403);
  }
  return {
    subject: delegatedIdentity.subject,
    email: delegatedIdentity.email
  };
}
__name(extractDelegatedAccessIdentity, "extractDelegatedAccessIdentity");
async function extractAccessIdentity(c) {
  if (c.env.ADMIN_AUTH_MODE === "LOCAL_HEADER") {
    const accessSubject = c.req.header("Cf-Access-Subject") ?? null;
    if (!accessSubject) {
      return c.json({ error: "ADMIN_ACCESS_SUBJECT_REQUIRED" }, 401);
    }
    return { subject: accessSubject, email: null };
  } else if (c.env.ADMIN_ACCESS_ISSUER.length > 0 && c.env.ADMIN_ACCESS_AUD.length > 0) {
    const token = c.req.header("Cf-Access-Jwt-Assertion");
    if (!token) {
      return c.json({ error: "ADMIN_ACCESS_JWT_REQUIRED" }, 401);
    }
    const accessIdentity = await verifyAccessJwt(token, c.env.ADMIN_ACCESS_ISSUER, c.env.ADMIN_ACCESS_AUD, {
      allowServiceToken: true
    });
    if (accessIdentity === null) {
      return c.json({ error: "ADMIN_ACCESS_JWT_INVALID" }, 403);
    }
    if (accessIdentity.kind === "service") {
      return extractDelegatedAccessIdentity(c);
    }
    return {
      subject: accessIdentity.subject,
      email: accessIdentity.email
    };
  } else {
    return c.json({ error: "ADMIN_ACCESS_JWT_VERIFICATION_NOT_CONFIGURED" }, 501);
  }
}
__name(extractAccessIdentity, "extractAccessIdentity");
async function authenticateAdmin(c) {
  const accessIdentity = await extractAccessIdentity(c);
  if (accessIdentity instanceof Response) return accessIdentity;
  const principal = await c.env.DB.prepare(
    `SELECT id, access_subject, role, status, display_name
     FROM admin_principals
     WHERE access_subject = ?
       AND status = 'ACTIVE'`
  ).bind(accessIdentity.subject).first();
  if (principal === null) {
    return c.json({ error: "ADMIN_PRINCIPAL_NOT_AUTHORIZED" }, 403);
  }
  return principal;
}
__name(authenticateAdmin, "authenticateAdmin");
function isJsonValue(value) {
  if (value === null) return true;
  if (typeof value === "boolean" || typeof value === "string") return true;
  if (typeof value === "number") return Number.isFinite(value);
  if (Array.isArray(value)) return value.every((item) => isJsonValue(item));
  if (typeof value === "object") return Object.values(value).every((item) => isJsonValue(item));
  return false;
}
__name(isJsonValue, "isJsonValue");
function isJsonObject(value) {
  return value !== null && typeof value === "object" && !Array.isArray(value) && isJsonValue(value);
}
__name(isJsonObject, "isJsonObject");
function isPositiveInteger(value) {
  return typeof value === "number" && Number.isSafeInteger(value) && value > 0;
}
__name(isPositiveInteger, "isPositiveInteger");
function isNonNegativeInteger(value) {
  return typeof value === "number" && Number.isSafeInteger(value) && value >= 0;
}
__name(isNonNegativeInteger, "isNonNegativeInteger");
function d1Changes(result) {
  if (result === null || typeof result !== "object") return null;
  const meta = "meta" in result ? result.meta : null;
  if (meta === null || typeof meta !== "object") return null;
  const changes = "changes" in meta ? meta.changes : null;
  return typeof changes === "number" ? changes : null;
}
__name(d1Changes, "d1Changes");
function toIso(timestampMs) {
  return new Date(timestampMs).toISOString();
}
__name(toIso, "toIso");
function mapRunResult(status) {
  if (status === "ACTIVE" || status === "FINISH_PENDING") return "IN_PROGRESS";
  if (status === "COMPLETED") return "CLEARED";
  if (status === "QUARANTINED") return "INVALIDATED";
  return "FAILED";
}
__name(mapRunResult, "mapRunResult");
function normalizeRiskTier(risk) {
  if (risk === "II" || risk === "III" || risk === "IV" || risk === "V") return risk;
  return "I";
}
__name(normalizeRiskTier, "normalizeRiskTier");
function suspiciousPriority(openSignals, hardViolations, evidenceCodes = []) {
  if (evidenceCodes.some(
    (code) => code === "CHECKSUM_MISMATCH" || code === "IMPOSSIBLE_SCORE" || code === "IMPOSSIBLE_DAMAGE" || code === "BALANCE_VERSION_REWARD_MISMATCH"
  )) {
    return "CRITICAL";
  }
  if (evidenceCodes.some(
    (code) => code === "ABNORMAL_CLEAR_TIME" || code === "ABNORMAL_ULTIMATE_USAGE" || code === "RAID_SCORE_REPEAT"
  )) {
    return "HIGH";
  }
  if (hardViolations >= 3 || openSignals >= 10) return "CRITICAL";
  if (hardViolations > 0 || openSignals >= 3) return "HIGH";
  if (openSignals >= 2) return "MEDIUM";
  return "LOW";
}
__name(suspiciousPriority, "suspiciousPriority");
function suspiciousReviewStatus(value) {
  if (value === "REVIEWING" || value === "IN_REVIEW") return "REVIEWING";
  if (value === "CLEARED") return "CLEARED";
  if (value === "SCORE_EXCLUDED") return "SCORE_EXCLUDED";
  if (value === "REWARD_HELD") return "REWARD_HELD";
  if (value === "ESCALATED") return "ESCALATED";
  if (value === "ACTIONED" || value === "RESOLVED") return "ACTIONED";
  if (value === "FALSE_POSITIVE") return "FALSE_POSITIVE";
  return "NEW";
}
__name(suspiciousReviewStatus, "suspiciousReviewStatus");
function currentEnforcementStatus(type) {
  if (type === "REWARD_RESTRICTED") return "REWARD_HELD";
  if (type === "TEMP_SUSPENDED" || type === "PERMANENT_BAN") return "ACTIONED";
  return "NONE";
}
__name(currentEnforcementStatus, "currentEnforcementStatus");
function currentMarkOnlyStatus(status) {
  if (status === "REWARD_HELD") return "REWARD_HELD";
  if (status === "SCORE_EXCLUDED") return "SCORE_EXCLUDED";
  if (status === "ACTIONED") return "ACTIONED";
  return "NONE";
}
__name(currentMarkOnlyStatus, "currentMarkOnlyStatus");
function suspiciousRunType(runId) {
  return runId.includes("raid") ? "RAID_RUN" : "RUN";
}
__name(suspiciousRunType, "suspiciousRunType");
function syncLevelFrom(coreRows) {
  if (coreRows.length !== 5) return 1;
  return Math.min(...coreRows.map((row) => row.trained_level));
}
__name(syncLevelFrom, "syncLevelFrom");
function coreLevelSpreadFrom(coreRows) {
  if (coreRows.length !== 5) return 0;
  const levels = coreRows.map((row) => row.trained_level);
  return Math.max(...levels) - Math.min(...levels);
}
__name(coreLevelSpreadFrom, "coreLevelSpreadFrom");
function parseRunSnapshot(snapshotJson) {
  const parsed = JSON.parse(snapshotJson);
  if (!isJsonValue(parsed)) throw new Error("RUN_SNAPSHOT_INVALID_JSON");
  return parsed;
}
__name(parseRunSnapshot, "parseRunSnapshot");
function nullableJsonString(value) {
  return typeof value === "string" ? value : null;
}
__name(nullableJsonString, "nullableJsonString");
function nullableJsonBoolean(value) {
  return typeof value === "boolean" ? value : false;
}
__name(nullableJsonBoolean, "nullableJsonBoolean");
function parseAdminRunVersionSnapshotFromJson(json, key, expectedSource) {
  if (typeof json !== "string") return null;
  const parsed = parseRunSnapshot(json);
  if (!isJsonObject(parsed) || !isJsonObject(parsed[key])) return null;
  const versionSnapshot = parsed[key];
  if (versionSnapshot.source !== expectedSource) return null;
  if (!isJsonObject(versionSnapshot.clientInstalled)) return null;
  if (!isJsonObject(versionSnapshot.clientAdmission)) return null;
  if (!isJsonObject(versionSnapshot.serverAcceptedDeployment)) return null;
  if (!isJsonObject(versionSnapshot.contentRelease)) return null;
  return {
    source: expectedSource,
    clientInstalled: {
      appVersion: nullableJsonString(versionSnapshot.clientInstalled.appVersion),
      versionCode: nullableJsonString(versionSnapshot.clientInstalled.versionCode),
      platform: nullableJsonString(versionSnapshot.clientInstalled.platform),
      channel: nullableJsonString(versionSnapshot.clientInstalled.channel)
    },
    clientAdmission: {
      status: nullableJsonString(versionSnapshot.clientAdmission.status),
      minimumVersionCode: nullableJsonString(versionSnapshot.clientAdmission.minimumVersionCode),
      updateRequired: nullableJsonBoolean(versionSnapshot.clientAdmission.updateRequired)
    },
    serverAcceptedDeployment: {
      environment: nullableJsonString(versionSnapshot.serverAcceptedDeployment.environment),
      runtimeDeploymentId: nullableJsonString(versionSnapshot.serverAcceptedDeployment.runtimeDeploymentId)
    },
    contentRelease: {
      status: nullableJsonString(versionSnapshot.contentRelease.status),
      releaseId: nullableJsonString(versionSnapshot.contentRelease.releaseId),
      contentVersion: nullableJsonString(versionSnapshot.contentRelease.contentVersion),
      catalogHash: nullableJsonString(versionSnapshot.contentRelease.catalogHash)
    },
    capturedAt: nullableJsonString(versionSnapshot.capturedAt)
  };
}
__name(parseAdminRunVersionSnapshotFromJson, "parseAdminRunVersionSnapshotFromJson");
function parseAdminRunStartVersionSnapshot(snapshotJson) {
  return parseAdminRunVersionSnapshotFromJson(snapshotJson, "runStartVersionSnapshot", "server_accepted_run_start") ?? parseAdminRunVersionSnapshotFromJson(snapshotJson, "runVersionSnapshot", "server_accepted_run_start");
}
__name(parseAdminRunStartVersionSnapshot, "parseAdminRunStartVersionSnapshot");
function parseAdminRunFinishVersionSnapshot(finalPayloadJson) {
  return parseAdminRunVersionSnapshotFromJson(finalPayloadJson, "runFinishVersionSnapshot", "server_accepted_run_finish");
}
__name(parseAdminRunFinishVersionSnapshot, "parseAdminRunFinishVersionSnapshot");
function adminRunStartVersionSnapshotStatus(snapshotJson) {
  return parseAdminRunStartVersionSnapshot(snapshotJson) === null ? "UNRECORDED_LEGACY" : "AVAILABLE";
}
__name(adminRunStartVersionSnapshotStatus, "adminRunStartVersionSnapshotStatus");
function adminRunFinishVersionSnapshotStatus(row) {
  if (row.finished_at === null) return "NOT_FINISHED";
  return parseAdminRunFinishVersionSnapshot(row.final_payload_json) === null ? "UNRECORDED_LEGACY" : "AVAILABLE";
}
__name(adminRunFinishVersionSnapshotStatus, "adminRunFinishVersionSnapshotStatus");
function parseJsonValue(json) {
  const parsed = JSON.parse(json);
  if (!isJsonValue(parsed)) throw new Error("JSON_VALUE_INVALID");
  return parsed;
}
__name(parseJsonValue, "parseJsonValue");
function parseJsonStringArray(value) {
  try {
    const parsed = JSON.parse(value);
    if (!Array.isArray(parsed)) return [];
    return parsed.filter((item) => typeof item === "string");
  } catch {
    return [];
  }
}
__name(parseJsonStringArray, "parseJsonStringArray");
function storyProgressOrder(storyProgressId) {
  const index = ACT_ONE_STORY_PROGRESS_ORDER.indexOf(storyProgressId);
  return index < 0 ? 0 : index;
}
__name(storyProgressOrder, "storyProgressOrder");
function sortActOneOperationIds(values) {
  return [...new Set(values)].filter((value) => ACT_ONE_OPERATION_IDS.includes(value)).sort((left, right) => storyProgressOrder(left) - storyProgressOrder(right));
}
__name(sortActOneOperationIds, "sortActOneOperationIds");
function narrativeMetadata(narrativeVersion) {
  return {
    narrativeVersion,
    contentStatus: "CANDIDATE",
    localizationStatus: "PENDING",
    fixtureStatus: "PARTIAL",
    missingContentStatus: "UNKNOWN"
  };
}
__name(narrativeMetadata, "narrativeMetadata");
function actOneNarrativeCoverageItems() {
  return ACT_ONE_NARRATIVE_COVERAGE.map((item) => ({
    missionId: item.missionId,
    displayName: item.displayName,
    contentStatus: "draft_exists",
    localizationStatus: "localization_pending",
    fixtureStatus: "fixture_pending",
    missingContentStatus: "version_pending",
    narrativeVersion: ACT_ONE_NARRATIVE_COVERAGE_VERSION,
    updatedAt: null,
    source: "narrative_doc_candidate"
  }));
}
__name(actOneNarrativeCoverageItems, "actOneNarrativeCoverageItems");
function actOneNarrativeCoverageSummary(versionMismatchCount = 0) {
  const items = actOneNarrativeCoverageItems();
  return {
    contentMissingCount: items.filter((item) => item.contentStatus === "content_missing").length,
    localizationPendingCount: items.filter((item) => item.localizationStatus === "localization_pending").length,
    fixturePendingCount: items.filter((item) => item.fixtureStatus === "fixture_pending").length,
    versionMismatchCount,
    latestNarrativeVersion: ACT_ONE_NARRATIVE_COVERAGE_VERSION
  };
}
__name(actOneNarrativeCoverageSummary, "actOneNarrativeCoverageSummary");
function emptyNarrativeCoverageSummary() {
  return {
    contentMissingCount: 0,
    localizationPendingCount: 0,
    fixturePendingCount: 0,
    versionMismatchCount: 0,
    latestNarrativeVersion: null
  };
}
__name(emptyNarrativeCoverageSummary, "emptyNarrativeCoverageSummary");
function storyProgressSummary(row) {
  const narrativeVersion = row.narrative_version ?? ACT_ONE_NARRATIVE_VERSION;
  const storyFlags = parseJsonStringArray(row.story_flags_json);
  return {
    accountId: row.account_id,
    actId: row.act_id,
    operationId: row.highest_story_progress_id,
    storyProgress: {
      highestStoryProgressId: row.highest_story_progress_id,
      highestClearedOperationId: row.highest_cleared_operation_id,
      order: storyProgressOrder(row.highest_story_progress_id)
    },
    firstClear: null,
    clearedOperationIds: sortActOneOperationIds(parseJsonStringArray(row.cleared_operations_json)),
    narrativeFlags: storyFlags,
    storyFlags,
    narrativeVersion,
    narrative: narrativeMetadata(narrativeVersion),
    lastProgressRunId: row.last_progress_run_id,
    updatedAt: toIso(row.updated_at),
    supportContext: {
      source: "account_story_progress",
      readOnly: true,
      mutationEndpointsEnabled: false
    }
  };
}
__name(storyProgressSummary, "storyProgressSummary");
function defaultStoryProgressDetail(accountId) {
  return {
    accountId,
    actId: "act_01_wasteland_railway",
    operationId: null,
    storyProgress: {
      highestStoryProgressId: "none",
      highestClearedOperationId: null,
      order: 0
    },
    firstClear: null,
    clearedOperationIds: [],
    narrativeFlags: [],
    storyFlags: [],
    unlockedOperations: ["opn_01_last_departure"],
    narrativeVersion: ACT_ONE_NARRATIVE_VERSION,
    narrative: narrativeMetadata(ACT_ONE_NARRATIVE_VERSION),
    lastProgressRunId: null,
    updatedAt: null,
    supportContext: {
      source: "default_none_progress",
      readOnly: true,
      mutationEndpointsEnabled: false
    }
  };
}
__name(defaultStoryProgressDetail, "defaultStoryProgressDetail");
function storyProgressDetail(row, unlockRows) {
  const unlockedOperations = sortActOneOperationIds([
    "opn_01_last_departure",
    ...unlockRows.map((unlockRow) => unlockRow.unlock_id.startsWith("operation:") ? unlockRow.unlock_id.slice("operation:".length) : null).filter((value) => value !== null)
  ]);
  return {
    ...storyProgressSummary(row),
    unlockedOperations
  };
}
__name(storyProgressDetail, "storyProgressDetail");
function offlineRewardSummary(row) {
  const storyProgressId = row.highest_story_progress_id ?? row.last_story_progress_id ?? "none";
  const narrativeVersion = row.narrative_version ?? row.last_narrative_version ?? ACT_ONE_NARRATIVE_VERSION;
  const hasOfflineState = row.last_offline_reward_claim_at !== null;
  return {
    accountId: row.account_id,
    displayName: row.display_name,
    accountStatus: row.account_status,
    storyProgressId,
    narrativeVersion,
    offlineRewardState: {
      source: hasOfflineState ? "account_offline_reward_state" : "accounts.created_at_fallback",
      lastOfflineRewardClaimAt: toIso(row.last_offline_reward_claim_at ?? row.account_created_at),
      lastStoryProgressId: row.last_story_progress_id ?? "none",
      lastBalanceConfigVersion: row.last_balance_config_version,
      lastNarrativeVersion: row.last_narrative_version ?? ACT_ONE_NARRATIVE_VERSION,
      lastPreviewedAt: row.last_previewed_at === null ? null : toIso(row.last_previewed_at),
      revision: row.revision ?? 0,
      updatedAt: row.state_updated_at === null ? null : toIso(row.state_updated_at)
    },
    lastClaim: row.last_claim_id === null ? null : {
      offlineRewardClaimId: row.last_claim_id,
      claimedAt: row.last_claim_created_at === null ? null : toIso(row.last_claim_created_at)
    },
    supportContext: {
      readOnly: true,
      claimMutationEnabled: false,
      mutationEndpointsEnabled: false
    }
  };
}
__name(offlineRewardSummary, "offlineRewardSummary");
function supportOfflineRewardFallback(account) {
  return {
    account_id: account.account_id,
    display_name: account.display_name,
    account_status: account.account_status,
    account_created_at: account.account_created_at,
    highest_story_progress_id: null,
    narrative_version: null,
    last_offline_reward_claim_at: null,
    last_story_progress_id: null,
    last_balance_config_version: null,
    last_narrative_version: null,
    last_previewed_at: null,
    revision: null,
    state_updated_at: null,
    last_claim_id: null,
    last_claim_created_at: null
  };
}
__name(supportOfflineRewardFallback, "supportOfflineRewardFallback");
function jsonObjectString(value, key) {
  if (!isJsonObject(value)) return null;
  const field = value[key];
  return typeof field === "string" && field.length > 0 ? field : null;
}
__name(jsonObjectString, "jsonObjectString");
function jsonObjectStringArray(value, key) {
  if (!isJsonObject(value) || !Array.isArray(value[key])) return [];
  return value[key].filter((item) => typeof item === "string" && item.length > 0);
}
__name(jsonObjectStringArray, "jsonObjectStringArray");
function jsonObjectPositiveInteger(value, key) {
  if (!isJsonObject(value)) return null;
  const field = value[key];
  return typeof field === "number" && Number.isInteger(field) && field > 0 ? field : null;
}
__name(jsonObjectPositiveInteger, "jsonObjectPositiveInteger");
function jsonObjectNonNegativeInteger(value, key) {
  if (!isJsonObject(value)) return null;
  const field = value[key];
  return typeof field === "number" && Number.isInteger(field) && field >= 0 ? field : null;
}
__name(jsonObjectNonNegativeInteger, "jsonObjectNonNegativeInteger");
function jsonObjectBoolean(value, key) {
  if (!isJsonObject(value)) return null;
  const field = value[key];
  return typeof field === "boolean" ? field : null;
}
__name(jsonObjectBoolean, "jsonObjectBoolean");
function parseRunPreviewMetadata(previewJson) {
  if (previewJson === null) {
    return {
      operationId: null,
      actId: null,
      regionId: null,
      encounterType: "ORDINARY_15_WAVE",
      raidId: null,
      bossId: null,
      bossPhaseRefs: [],
      allowedOperatorSlots: null,
      startAugmentPackageRef: null,
      startTacticPackageRef: null,
      contentVersion: null
    };
  }
  try {
    const parsed = parseJsonValue(previewJson);
    const encounterType = isJsonObject(parsed) && parsed.encounterType === "RAID_BOSS" ? "RAID_BOSS" : "ORDINARY_15_WAVE";
    return {
      operationId: jsonObjectString(parsed, "operationId"),
      actId: jsonObjectString(parsed, "actId"),
      regionId: jsonObjectString(parsed, "regionId"),
      encounterType,
      raidId: encounterType === "RAID_BOSS" ? jsonObjectString(parsed, "raidId") : null,
      bossId: encounterType === "RAID_BOSS" ? jsonObjectString(parsed, "bossId") : null,
      bossPhaseRefs: encounterType === "RAID_BOSS" ? jsonObjectStringArray(parsed, "bossPhaseRefs") : [],
      allowedOperatorSlots: jsonObjectPositiveInteger(parsed, "allowedOperatorSlots"),
      startAugmentPackageRef: jsonObjectString(parsed, "startAugmentPackageRef"),
      startTacticPackageRef: jsonObjectString(parsed, "startTacticPackageRef"),
      contentVersion: jsonObjectString(parsed, "contentVersion")
    };
  } catch {
    return {
      operationId: null,
      actId: null,
      regionId: null,
      encounterType: "ORDINARY_15_WAVE",
      raidId: null,
      bossId: null,
      bossPhaseRefs: [],
      allowedOperatorSlots: null,
      startAugmentPackageRef: null,
      startTacticPackageRef: null,
      contentVersion: null
    };
  }
}
__name(parseRunPreviewMetadata, "parseRunPreviewMetadata");
function parseRewardLineSummary(rewardPlanJson) {
  try {
    const parsed = parseJsonValue(rewardPlanJson);
    if (!isJsonObject(parsed) || !Array.isArray(parsed.rewardLines)) return [];
    return parsed.rewardLines.filter((line) => {
      if (!isJsonObject(line)) return false;
      return typeof line.kind === "string" && typeof line.id === "string" && typeof line.quantity === "number";
    });
  } catch {
    return [];
  }
}
__name(parseRewardLineSummary, "parseRewardLineSummary");
function parseGachaPostPityState(resultSummaryJson) {
  try {
    const parsed = parseJsonValue(resultSummaryJson);
    if (!isJsonObject(parsed)) return null;
    return isJsonValue(parsed.postPityState) ? parsed.postPityState : null;
  } catch {
    return null;
  }
}
__name(parseGachaPostPityState, "parseGachaPostPityState");
function parseGachaGuaranteeTypes(resultSummaryJson) {
  try {
    const parsed = parseJsonValue(resultSummaryJson);
    if (!isJsonObject(parsed) || !Array.isArray(parsed.responseResults)) return [];
    return [...new Set(parsed.responseResults.map((result) => isJsonObject(result) && typeof result.guaranteeType === "string" ? result.guaranteeType : null).filter((value) => value !== null))];
  } catch {
    return [];
  }
}
__name(parseGachaGuaranteeTypes, "parseGachaGuaranteeTypes");
function supportLoginBonusSummary(row, idempotencyRows) {
  return {
    bonusId: LAUNCH_LOGIN_BONUS_ID,
    claimStatus: row === null ? "UNCLAIMED" : "CLAIMED",
    claimedAt: row === null ? null : toIso(row.claimed_at),
    rewardTransactionId: row?.reward_transaction_id ?? null,
    expectedRewardLines: LAUNCH_LOGIN_BONUS_EXPECTED_REWARD_LINES,
    rewardLineSummary: row === null ? [] : parseRewardLineSummary(row.reward_json),
    idempotencyReplayKnown: row === null ? false : idempotencyRows.some((idem) => idem.operation === "login_bonus_claim" && idem.result_ref === row.reward_transaction_id),
    readOnly: true
  };
}
__name(supportLoginBonusSummary, "supportLoginBonusSummary");
function supportExplanationTemplates() {
  return [
    {
      caseType: "offline_reward",
      playerFacingSummary: "\uC624\uD504\uB77C\uC778 \uBCF4\uC0C1\uC740 \uCD5C\uC18C \uB204\uC801 \uC2DC\uAC04\uC774 \uC9C0\uB098\uC57C \uC218\uB839\uD560 \uC218 \uC788\uC2B5\uB2C8\uB2E4. \uC11C\uBC84 \uAE30\uB85D \uAE30\uC900\uC73C\uB85C \uC218\uB839 \uAC00\uB2A5 \uC0C1\uD0DC\uB97C \uD655\uC778\uD574 \uC8FC\uC138\uC694.",
      escalationNeeded: false
    },
    {
      caseType: "duplicate_replay",
      playerFacingSummary: "\uC11C\uBC84\uAC00 \uC774\uC804 \uC694\uCCAD\uC744 \uC774\uBBF8 \uCC98\uB9AC\uD55C \uAE30\uB85D\uC774 \uC788\uC2B5\uB2C8\uB2E4. \uC571\uC744 \uC0C8\uB85C\uACE0\uCE68\uD558\uAC70\uB098 \uACC4\uC815 \uC0C1\uD0DC \uB3D9\uAE30\uD654\uB97C \uC2DC\uB3C4\uD574 \uC8FC\uC138\uC694.",
      escalationNeeded: false
    },
    {
      caseType: "integrity_restriction",
      playerFacingSummary: "\uD574\uB2F9 \uAE30\uB85D\uC740 \uC6B4\uC601 \uAC80\uD1A0\uAC00 \uD544\uC694\uD55C \uC0C1\uD0DC\uC785\uB2C8\uB2E4. \uC0C1\uC138 \uD655\uC778 \uD6C4 \uBCC4\uB3C4\uB85C \uC548\uB0B4\uB4DC\uB9AC\uACA0\uC2B5\uB2C8\uB2E4.",
      escalationNeeded: true
    }
  ];
}
__name(supportExplanationTemplates, "supportExplanationTemplates");
function getSupportPrimaryProvider(providers) {
  if (providers.includes("GOOGLE")) return "GOOGLE";
  if (providers.includes("GUEST")) return "GUEST";
  return providers[0] ?? null;
}
__name(getSupportPrimaryProvider, "getSupportPrimaryProvider");
function toNullableIso(ms) {
  return ms === null || ms === void 0 ? null : toIso(ms);
}
__name(toNullableIso, "toNullableIso");
function getLastLoginAgeSeconds(lastLoginAt, now) {
  if (lastLoginAt === null || lastLoginAt === void 0) return null;
  return Math.max(0, Math.floor((now - lastLoginAt) / 1e3));
}
__name(getLastLoginAgeSeconds, "getLastLoginAgeSeconds");
function countValue(value) {
  return typeof value === "number" && Number.isFinite(value) ? value : 0;
}
__name(countValue, "countValue");
function kstCalendarDayStartUtcMs(now) {
  const kstOffsetMs = 9 * 60 * 60 * 1e3;
  const kstDate = new Date(now + kstOffsetMs);
  return Date.UTC(kstDate.getUTCFullYear(), kstDate.getUTCMonth(), kstDate.getUTCDate()) - kstOffsetMs;
}
__name(kstCalendarDayStartUtcMs, "kstCalendarDayStartUtcMs");
function parseOptionalJsonObject(json) {
  try {
    const parsed = parseJsonValue(json);
    return isJsonObject(parsed) ? parsed : {};
  } catch {
    return {};
  }
}
__name(parseOptionalJsonObject, "parseOptionalJsonObject");
function opsErrorLogLinks(row) {
  return {
    accountId: row.account_id,
    runId: row.run_id,
    rewardPreviewId: row.reward_preview_id,
    rewardClaimId: row.reward_claim_id,
    offlineRewardClaimId: row.offline_reward_claim_id,
    gachaBatchId: row.gacha_batch_id,
    gachaBannerId: row.gacha_banner_id,
    integrityEventId: row.integrity_event_id,
    supportTicketId: row.support_ticket_id
  };
}
__name(opsErrorLogLinks, "opsErrorLogLinks");
function opsErrorLogSummary(row) {
  const context = parseOptionalJsonObject(row.sanitized_context_json);
  const serverVersion = context && typeof context === "object" && !Array.isArray(context) && typeof context.serverVersion === "string" ? context.serverVersion : null;
  const appVersion = context && typeof context === "object" && !Array.isArray(context) && typeof context.appVersion === "string" ? context.appVersion : null;
  const appVersionCode = context && typeof context === "object" && !Array.isArray(context) && typeof context.appVersionCode === "string" ? context.appVersionCode : null;
  return {
    id: row.id,
    occurredAt: toIso(row.occurred_at),
    environment: row.environment,
    service: row.service,
    severity: row.severity,
    errorCode: row.error_code,
    messageSummary: row.message_summary,
    status: row.status,
    source: row.source,
    requestId: row.request_id,
    serverVersion,
    appVersion,
    appVersionCode,
    accountId: row.account_id,
    runId: row.run_id,
    rewardPreviewId: row.reward_preview_id,
    rewardClaimId: row.reward_claim_id,
    offlineRewardClaimId: row.offline_reward_claim_id,
    gachaBatchId: row.gacha_batch_id,
    gachaBannerId: row.gacha_banner_id,
    storyProgressId: row.story_progress_id,
    integrityEventId: row.integrity_event_id,
    supportTicketId: row.support_ticket_id,
    idempotencyOperation: row.idempotency_operation,
    idempotencyKeyHash: row.idempotency_key_hash,
    retryable: row.retryable === 1,
    masked: row.masked === 1,
    payloadRef: row.payload_ref,
    traceRef: row.trace_ref,
    resolvedAt: row.resolved_at === null ? null : toIso(row.resolved_at),
    resolvedBy: row.resolved_by,
    resolutionNote: row.resolution_note,
    createdAt: toIso(row.created_at),
    links: opsErrorLogLinks(row)
  };
}
__name(opsErrorLogSummary, "opsErrorLogSummary");
function opsErrorLogDetail(row) {
  return {
    ...opsErrorLogSummary(row),
    correlation: parseOptionalJsonObject(row.correlation_json),
    sanitizedContext: parseOptionalJsonObject(row.sanitized_context_json)
  };
}
__name(opsErrorLogDetail, "opsErrorLogDetail");
function supportOpsErrorSummary(row) {
  const context = parseOptionalJsonObject(row.sanitized_context_json);
  const serverVersion = context && typeof context === "object" && !Array.isArray(context) && typeof context.serverVersion === "string" ? context.serverVersion : null;
  const appVersion = context && typeof context === "object" && !Array.isArray(context) && typeof context.appVersion === "string" ? context.appVersion : null;
  const appVersionCode = context && typeof context === "object" && !Array.isArray(context) && typeof context.appVersionCode === "string" ? context.appVersionCode : null;
  return {
    errorLogId: row.id,
    occurredAt: toIso(row.occurred_at),
    environment: row.environment,
    service: row.service,
    severity: row.severity,
    errorCode: row.error_code,
    messageSummary: row.message_summary,
    status: row.status,
    source: row.source,
    requestId: row.request_id,
    serverVersion,
    appVersion,
    appVersionCode,
    accountId: row.account_id,
    runId: row.run_id,
    rewardPreviewId: row.reward_preview_id,
    rewardClaimId: row.reward_claim_id,
    offlineRewardClaimId: row.offline_reward_claim_id,
    gachaBatchId: row.gacha_batch_id,
    gachaBannerId: row.gacha_banner_id,
    storyProgressId: row.story_progress_id,
    integrityEventId: row.integrity_event_id,
    supportTicketId: row.support_ticket_id,
    idempotencyOperation: row.idempotency_operation,
    idempotencyKeyHash: row.idempotency_key_hash,
    retryable: row.retryable === 1,
    masked: row.masked === 1,
    payloadRef: row.payload_ref,
    traceRef: row.trace_ref,
    links: opsErrorLogLinks(row)
  };
}
__name(supportOpsErrorSummary, "supportOpsErrorSummary");
function supportTicketSummary(row) {
  return {
    ticketId: row.id,
    accountId: row.account_id,
    category: row.category,
    status: row.status,
    bodyPreview: row.body_preview,
    bodyLength: row.body_length,
    detailAvailable: true,
    appVersion: row.app_version,
    appVersionCode: row.app_version_code,
    clientComposedAt: toNullableIso(row.client_composed_at),
    clientSubmittedAt: toNullableIso(row.client_submitted_at),
    serverReceivedAt: toIso(row.server_received_at),
    createdAt: toIso(row.created_at),
    updatedAt: toIso(row.updated_at)
  };
}
__name(supportTicketSummary, "supportTicketSummary");
function supportTicketDetail(row) {
  return {
    ...supportTicketSummary(row),
    body: row.body_text,
    idempotencyReplayKnown: row.idempotency_replay_known === 1
  };
}
__name(supportTicketDetail, "supportTicketDetail");
function parseJsonObjectOrEmpty(json) {
  try {
    const parsed = parseJsonValue(json);
    return isJsonObject(parsed) ? parsed : {};
  } catch {
    return {};
  }
}
__name(parseJsonObjectOrEmpty, "parseJsonObjectOrEmpty");
function getJsonNumber(value, keys, fallback) {
  for (const key of keys) {
    const field = value[key];
    if (typeof field === "number" && Number.isSafeInteger(field) && field > 0) return field;
  }
  return fallback;
}
__name(getJsonNumber, "getJsonNumber");
function parseAdminGachaPityThresholds(pityConfigJson) {
  const parsed = parseJsonObjectOrEmpty(pityConfigJson);
  return {
    rareOrHigher: getJsonNumber(parsed, ["rare", "rareOrHigher", "rareOrHigherGuarantee"], 10),
    elite: getJsonNumber(parsed, ["elite", "eliteGuarantee"], 30),
    pickupElite: getJsonNumber(parsed, ["pickup", "pickupElite", "pickupEliteGuarantee"], 60)
  };
}
__name(parseAdminGachaPityThresholds, "parseAdminGachaPityThresholds");
function parseAdminGachaPoolDisplay(poolSnapshotJson) {
  const parsed = parseJsonObjectOrEmpty(poolSnapshotJson);
  const cost = isJsonObject(parsed.cost) ? parsed.cost : {};
  const quantity = cost.quantityPerPull ?? cost.quantity;
  const pullCountOptions = Array.isArray(parsed.allowedPullCounts) ? parsed.allowedPullCounts : Array.isArray(parsed.pullCountOptions) ? parsed.pullCountOptions : [1, 10];
  return {
    pullCostItemId: typeof cost.itemId === "string" && cost.itemId.length > 0 ? cost.itemId : "cur_command_ticket",
    pullCostQuantity: typeof quantity === "number" && Number.isSafeInteger(quantity) && quantity > 0 ? quantity : 1,
    pullCountOptions: pullCountOptions.filter((value) => typeof value === "number" && (value === 1 || value === 10)).sort((left, right) => left - right)
  };
}
__name(parseAdminGachaPoolDisplay, "parseAdminGachaPoolDisplay");
function adminGachaBannerSummary(row) {
  const pool = parseAdminGachaPoolDisplay(row.pool_snapshot_json);
  return {
    bannerId: row.id,
    bannerType: row.banner_type,
    status: row.status,
    activeBannerId: row.status === "ACTIVE" ? row.id : null,
    pickupOperatorId: row.pickup_operator_id,
    balanceVersion: String(row.balance_config_version),
    pullCostItemId: pool.pullCostItemId,
    pullCostQuantity: pool.pullCostQuantity,
    pullCountOptions: pool.pullCountOptions.length > 0 ? pool.pullCountOptions : [1, 10],
    pityThresholds: parseAdminGachaPityThresholds(row.pity_config_json),
    guaranteeTypes: ["NONE", "RARE_OR_HIGHER_10", "ELITE_30", "PICKUP_ELITE_60"],
    startsAt: toIso(row.starts_at),
    endsAt: row.ends_at === null ? null : toIso(row.ends_at),
    updatedAt: toIso(row.updated_at),
    detailAvailable: true
  };
}
__name(adminGachaBannerSummary, "adminGachaBannerSummary");
function adminGachaPitySummary(row, accountId, bannerId) {
  return {
    accountId,
    bannerId,
    pityState: {
      totalPullCount: row?.total_pull_count ?? 0,
      elitePityCount: row?.elite_pity_count ?? 0,
      pickupElitePityCount: row?.pickup_elite_pity_count ?? 0,
      rareOrHigherPityCount: row?.rare_or_higher_pity_count ?? 0,
      revision: row?.revision ?? 0,
      lastBatchId: row?.last_batch_id ?? null,
      updatedAt: row === null ? null : toIso(row.updated_at)
    },
    detailAvailable: row !== null,
    supportContext: {
      readOnly: true,
      mutationEndpointsEnabled: false,
      source: "player_gacha_pity"
    }
  };
}
__name(adminGachaPitySummary, "adminGachaPitySummary");
function parseGachaResponseResults(resultSummaryJson) {
  const parsed = parseJsonObjectOrEmpty(resultSummaryJson);
  if (!Array.isArray(parsed.responseResults)) return [];
  return parsed.responseResults.filter((value) => isJsonObject(value));
}
__name(parseGachaResponseResults, "parseGachaResponseResults");
function adminGachaBatchSummary(row) {
  return {
    batchId: row.batch_id,
    accountId: row.account_id,
    bannerId: row.banner_id,
    resultStatus: "ACCEPTED",
    idempotencyReplayKnown: row.idempotency_replay_known === 1,
    pullCount: row.pull_count,
    cost: {
      itemId: row.cost_item_id,
      quantity: row.cost_quantity,
      itemLedgerRef: row.cost_ledger_id ?? null
    },
    prePityState: parseOptionalJsonObject(row.pre_pity_json),
    postPityState: parseOptionalJsonObject(row.post_pity_json),
    responseResults: parseGachaResponseResults(row.result_summary_json),
    guaranteeTypes: parseGachaGuaranteeTypes(row.result_summary_json),
    createdAt: toIso(row.created_at)
  };
}
__name(adminGachaBatchSummary, "adminGachaBatchSummary");
function parseDuplicateConversion(value) {
  if (value === null) return null;
  const parsed = parseJsonObjectOrEmpty(value);
  const itemId = typeof parsed.itemId === "string" ? parsed.itemId : null;
  const operatorId = typeof parsed.operatorId === "string" ? parsed.operatorId : null;
  return {
    displayItemId: itemId,
    operatorId,
    persistedTokenPattern: itemId !== null && operatorId !== null ? `${itemId}:${operatorId}` : null,
    quantity: typeof parsed.quantity === "number" && Number.isFinite(parsed.quantity) ? parsed.quantity : null
  };
}
__name(parseDuplicateConversion, "parseDuplicateConversion");
function adminGachaResultSummary(row, quantity = null) {
  return {
    resultId: row.result_id,
    pullSequence: row.pull_sequence,
    resultRarity: row.result_rarity,
    resultType: row.result_type,
    reward: {
      id: row.reward_id,
      quantity
    },
    isPickup: row.is_pickup === 1,
    wasDuplicate: row.was_duplicate === 1,
    guaranteeType: row.guarantee_type,
    duplicateConversion: parseDuplicateConversion(row.duplicate_conversion_json),
    itemLedgerRef: row.item_ledger_id,
    operatorLedgerRef: row.progression_ledger_id
  };
}
__name(adminGachaResultSummary, "adminGachaResultSummary");
function adminGachaBatchCauseEvidence(batch, resultItems, itemLedgerRefs, operatorLedgerRefs) {
  const duplicateConversionItems = resultItems.map((result) => result.duplicateConversion).filter((conversion) => conversion !== null);
  return {
    source: "gacha_pull_batch_server_records",
    accountId: batch.account_id,
    batchId: batch.batch_id,
    bannerId: batch.banner_id,
    resultStatus: "ACCEPTED",
    idempotencyReplayKnown: batch.idempotency_replay_known === 1,
    guaranteeTypes: parseGachaGuaranteeTypes(batch.result_summary_json),
    prePityState: parseOptionalJsonObject(batch.pre_pity_json),
    postPityState: parseOptionalJsonObject(batch.post_pity_json),
    duplicateResultCount: resultItems.filter((result) => result.wasDuplicate).length,
    duplicateConversionItems,
    cost: {
      itemId: batch.cost_item_id,
      quantity: batch.cost_quantity,
      itemLedgerRef: batch.cost_ledger_id ?? null
    },
    resultLedgerRefs: {
      itemLedgerRefs,
      operatorLedgerRefs
    },
    rawProbabilityAvailable: false,
    fullWeightsAvailable: false,
    rngMaterialAvailable: false
  };
}
__name(adminGachaBatchCauseEvidence, "adminGachaBatchCauseEvidence");
function parseRunFinalPayloadMetadata(payloadJson) {
  if (typeof payloadJson !== "string") {
    return {
      selectedAugmentIds: [],
      selectedTacticIds: [],
      raidId: null,
      bossId: null,
      bossDefeated: null,
      completedPhaseCount: null,
      encounterDurationSeconds: null,
      staminaRefund: null
    };
  }
  const parsed = parseJsonObjectOrEmpty(payloadJson);
  const raidResult = isJsonObject(parsed.raidResult) ? parsed.raidResult : {};
  const directTacticIds = Array.isArray(parsed.selectedTacticIds) ? parsed.selectedTacticIds.filter((value) => typeof value === "string" && value.length > 0) : [];
  const selectedTacticIds = directTacticIds.length > 0 ? directTacticIds : Array.isArray(parsed.tacticSelections) ? parsed.tacticSelections.map((selection) => isJsonObject(selection) ? jsonObjectString(selection, "selectedTacticId") : null).filter((value) => value !== null) : [];
  const selectedAugmentIds = Array.isArray(parsed.selectedAugmentIds) ? parsed.selectedAugmentIds.filter((value) => typeof value === "string" && value.length > 0) : Array.isArray(parsed.tacticSelections) ? parsed.tacticSelections.map((selection) => isJsonObject(selection) ? jsonObjectString(selection, "selectedAugmentId") : null).filter((value) => value !== null) : [];
  const refund = isJsonObject(parsed.staminaRefund) ? parsed.staminaRefund : null;
  const refundStatus = refund === null ? null : jsonObjectString(refund, "status");
  const refundReason = refund === null ? null : jsonObjectString(refund, "reason");
  return {
    selectedAugmentIds,
    selectedTacticIds,
    raidId: jsonObjectString(raidResult, "raidId"),
    bossId: jsonObjectString(raidResult, "bossId"),
    bossDefeated: jsonObjectBoolean(raidResult, "bossDefeated"),
    completedPhaseCount: jsonObjectNonNegativeInteger(raidResult, "completedPhaseCount"),
    encounterDurationSeconds: jsonObjectNonNegativeInteger(raidResult, "encounterDurationSeconds"),
    staminaRefund: refund === null ? null : {
      status: refundStatus,
      resource: jsonObjectString(refund, "refundType"),
      reason: refundReason,
      policyId: jsonObjectString(refund, "policyRef"),
      serverOwnedMutation: refundStatus === "COMMITTED",
      fuelLedgerReason: refundReason === "STORY_RAID_FAILURE" ? "STORY_RAID_FAILURE_REFUND" : null
    }
  };
}
__name(parseRunFinalPayloadMetadata, "parseRunFinalPayloadMetadata");
function parseRunRewardCalculationMetadata(rewardPlanJson) {
  if (typeof rewardPlanJson !== "string") {
    return { appliedMultiplierLabel: null, calculationMetadataStatus: "MISSING" };
  }
  const parsed = parseJsonObjectOrEmpty(rewardPlanJson);
  const calculation = isJsonObject(parsed.calculation) ? parsed.calculation : null;
  if (calculation === null) {
    return { appliedMultiplierLabel: null, calculationMetadataStatus: "MISSING" };
  }
  return {
    appliedMultiplierLabel: jsonObjectString(calculation, "appliedMultiplierLabel"),
    calculationMetadataStatus: "AVAILABLE"
  };
}
__name(parseRunRewardCalculationMetadata, "parseRunRewardCalculationMetadata");
function validSupportIdentityRows(rows) {
  return rows.filter(
    (row) => row !== null && row !== void 0 && typeof row.provider === "string"
  );
}
__name(validSupportIdentityRows, "validSupportIdentityRows");
function validOpsErrorRows(rows) {
  return rows.filter(
    (row) => row !== null && row !== void 0 && typeof row.id === "string"
  );
}
__name(validOpsErrorRows, "validOpsErrorRows");
function validFuelLedgerRows(rows) {
  return rows.filter(
    (row) => row !== null && row !== void 0 && typeof row.id === "string"
  );
}
__name(validFuelLedgerRows, "validFuelLedgerRows");
function rejectAdminDashboardBody(c) {
  return c.json({
    error: "contract_violation",
    message: "Admin dashboard read endpoints do not accept request bodies."
  }, 400);
}
__name(rejectAdminDashboardBody, "rejectAdminDashboardBody");
function rejectAdminGachaBody(c) {
  return c.json({
    error: "contract_violation",
    message: "Admin gacha read endpoints do not accept request bodies."
  }, 400);
}
__name(rejectAdminGachaBody, "rejectAdminGachaBody");
function rejectAdminPlayerBody(c) {
  return c.json({
    error: "contract_violation",
    message: "Admin player read endpoints do not accept request bodies."
  }, 400);
}
__name(rejectAdminPlayerBody, "rejectAdminPlayerBody");
function rejectAdminRunBody(c) {
  return c.json({
    error: "contract_violation",
    message: "Admin run read endpoints do not accept request bodies."
  }, 400);
}
__name(rejectAdminRunBody, "rejectAdminRunBody");
function hasRequestBody(c) {
  const contentLength = c.req.header("content-length");
  return contentLength !== void 0 && contentLength !== "0";
}
__name(hasRequestBody, "hasRequestBody");
function rejectAdminStoryProgressBody(c) {
  return c.json({
    error: "contract_violation",
    message: "Admin story progress read endpoints do not accept request bodies."
  }, 400);
}
__name(rejectAdminStoryProgressBody, "rejectAdminStoryProgressBody");
function rejectAdminOfflineRewardBody(c) {
  return c.json({
    error: "contract_violation",
    message: "Admin offline reward read endpoints do not accept request bodies."
  }, 400);
}
__name(rejectAdminOfflineRewardBody, "rejectAdminOfflineRewardBody");
function rejectAdminSupportBody(c) {
  return c.json({
    error: "contract_violation",
    message: "Admin support account summary endpoints do not accept request bodies."
  }, 400);
}
__name(rejectAdminSupportBody, "rejectAdminSupportBody");
function rejectAdminOpsErrorLogBody(c) {
  return c.json({
    error: "contract_violation",
    message: "Admin ops error log read endpoints do not accept request bodies."
  }, 400);
}
__name(rejectAdminOpsErrorLogBody, "rejectAdminOpsErrorLogBody");
function rejectAdminServerDeploymentBody(c) {
  return c.json({
    error: "contract_violation",
    message: "Admin server deployment read endpoints do not accept request bodies."
  }, 400);
}
__name(rejectAdminServerDeploymentBody, "rejectAdminServerDeploymentBody");
function hasAdminPermission(principal, permission) {
  if (permission === OPS_ERROR_LOG_READ_PERMISSION) {
    return principal.role === "OWNER" || principal.role === "OPERATOR";
  }
  if (permission === SERVER_DEPLOYMENT_READ_PERMISSION) {
    return principal.role === "OWNER" || principal.role === "OPERATOR" || principal.role === "VIEWER";
  }
  if (permission === SUPPORT_READ_PERMISSION) {
    return principal.role === "OWNER" || principal.role === "OPERATOR" || principal.role === "VIEWER";
  }
  if (permission === OFFLINE_REWARD_READ_PERMISSION) {
    return principal.role === "OWNER" || principal.role === "OPERATOR" || principal.role === "VIEWER";
  }
  if (permission === STORY_PROGRESS_READ_PERMISSION) {
    return principal.role === "OWNER" || principal.role === "OPERATOR" || principal.role === "VIEWER";
  }
  if (permission === ACCOUNT_NOTE_WRITE_PERMISSION) {
    return principal.role === "OWNER" || principal.role === "OPERATOR";
  }
  if (permission === BALANCE_PUBLISH_PERMISSION) {
    return principal.role === "OWNER";
  }
  return false;
}
__name(hasAdminPermission, "hasAdminPermission");
function parseServerDeploymentService(value) {
  return value === "game-api" || value === "admin-api" || value === "async-worker" ? value : null;
}
__name(parseServerDeploymentService, "parseServerDeploymentService");
function currentAdminEnvironment(c) {
  if (c.env.ENVIRONMENT === "DEV") return "dev";
  if (c.env.ENVIRONMENT === "PRD") return "prd";
  return null;
}
__name(currentAdminEnvironment, "currentAdminEnvironment");
function parseServerDeploymentEnvironment(value, fallback) {
  if (value === void 0) return fallback;
  const normalized = value.toLowerCase();
  return normalized === "dev" || normalized === "prd" ? normalized : null;
}
__name(parseServerDeploymentEnvironment, "parseServerDeploymentEnvironment");
function adminPermissionDenied() {
  return new Response(JSON.stringify({ error: "ADMIN_PERMISSION_DENIED" }), {
    status: 403,
    headers: { "Content-Type": "application/json" }
  });
}
__name(adminPermissionDenied, "adminPermissionDenied");
function parsePlayerNoteSummary(summaryJson) {
  const parsed = parseJsonValue(summaryJson);
  if (!isJsonObject(parsed) || typeof parsed.note !== "string" || parsed.note.length === 0) {
    throw new Error("PLAYER_NOTE_INVALID_JSON");
  }
  return parsed.note;
}
__name(parsePlayerNoteSummary, "parsePlayerNoteSummary");
async function createPayloadHash(payload) {
  return `sha256:${await sha256Hex(canonicalJson(payload))}`;
}
__name(createPayloadHash, "createPayloadHash");
async function createTokenHash(token) {
  return `sha256:${await sha256Hex(token)}`;
}
__name(createTokenHash, "createTokenHash");
async function createBalancePublishPayloadHash(draftId, expectedRevision, expectedChecksum) {
  return createPayloadHash({
    permission: BALANCE_PUBLISH_PERMISSION,
    draftId,
    expectedRevision,
    expectedChecksum
  });
}
__name(createBalancePublishPayloadHash, "createBalancePublishPayloadHash");
async function writeBalancePublishFailureAudit(c, principal, resourceId, errorCode, payloadHash, changeSummary) {
  await c.env.DB.prepare(
    `INSERT INTO admin_audit_logs (
       id,
       principal_id,
       role,
       permission,
       action,
       resource_type,
       resource_id,
       request_id,
       confirmation_id,
       payload_hash,
       change_summary_json,
       account_mutation_id,
       result,
       error_code,
       created_at
     ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, NULL, ?, ?, NULL, ?, ?, ?)`
  ).bind(
    `audit_${createRequestId().slice(4)}`,
    principal.id,
    principal.role,
    BALANCE_PUBLISH_PERMISSION,
    "BALANCE_CONFIG_PUBLISH",
    "balance_config_draft",
    resourceId,
    createRequestId(),
    payloadHash,
    canonicalJson(changeSummary),
    "FAILURE",
    errorCode,
    Date.now()
  ).run();
}
__name(writeBalancePublishFailureAudit, "writeBalancePublishFailureAudit");
app.get("/status", (c) => c.json({ ok: true, service: "admin-api", requestId: createRequestId() }));
app.get("/v1/admin/versions", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  c.header("Cache-Control", "no-store");
  if (c.env.ENVIRONMENT !== "DEV") return c.json({ error: { code: "version_environment_disabled" } }, 403);
  if (!c.env.VERSION_BUCKET) return c.json({ error: { code: "version_storage_unavailable" } }, 503);
  try {
    const snapshot = await readVersionSnapshot(c.env.VERSION_BUCKET);
    const version = snapshot.version;
    return c.json({
      environment: "DEV",
      server: "dev-admin.lastrail.app",
      canExport: principal.role === "OWNER" && !version?.clientReleaseRegistry,
      revision: snapshot.etag,
      version: version?.version ?? null,
      versionCode: version?.versionCode ?? null,
      minimumVersionCode: version?.minimumVersionCode ?? null,
      contentRelease: version?.contentRelease ?? null,
      admissionPolicy: {
        status: version?.minimumVersionCode === void 0 ? "UNCONFIGURED" : "CONFIGURED",
        gameApiEnforcement: "CLIENT_CONFIG_AND_PROTECTED_ROUTES"
      }
    });
  } catch {
    return c.json({ error: { code: "version_storage_read_failed" } }, 502);
  }
});
app.post("/v1/admin/versions/export", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  c.header("Cache-Control", "no-store");
  if (principal.role !== "OWNER" || c.env.ENVIRONMENT !== "DEV") return c.json({ error: { code: "version_export_forbidden" } }, 403);
  const body = await c.req.json().catch(() => null);
  const version = parseVersion(body);
  if (!version || !version.minimumVersionCode) return c.json({ error: { code: "invalid_version" } }, 400);
  if (!Object.prototype.hasOwnProperty.call(body, "expectedRevision")) {
    return c.json({ error: { code: "version_precondition_required" } }, 428);
  }
  const expectedRevision = body.expectedRevision;
  if (expectedRevision !== null && (typeof expectedRevision !== "string" || !/^"[\x21\x23-\x7e]{1,256}"$/u.test(expectedRevision))) {
    return c.json({ error: { code: "invalid_version_revision" } }, 400);
  }
  if (!c.env.VERSION_BUCKET) return c.json({ error: { code: "version_storage_unavailable" } }, 503);
  try {
    const snapshot = await readVersionSnapshot(c.env.VERSION_BUCKET);
    if (snapshot.etag !== expectedRevision) {
      return c.json({ error: { code: "version_current_pointer_conflict" } }, 409);
    }
    const existing = snapshot.version;
    if (existing?.clientReleaseRegistry || version.clientReleaseRegistry) return c.json({ error: { code: "version_registry_managed" } }, 409);
    const mergedVersion = version.contentRelease === void 0 && existing?.contentRelease !== void 0 ? { ...version, contentRelease: existing.contentRelease } : version;
    return c.json({ accepted: true, ...await saveVersion(c.env.VERSION_BUCKET, mergedVersion, { expectedEtag: snapshot.etag }) });
  } catch (error) {
    if (error instanceof Error && error.message === "version_current_pointer_conflict") {
      return c.json({ error: { code: "version_current_pointer_conflict" } }, 409);
    }
    return c.json({ error: { code: "version_storage_write_failed" } }, 502);
  }
});
app.get("/v1/admin/client-releases", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  c.header("Cache-Control", "no-store");
  const target = c.req.query("target");
  if (target !== "DEV" && target !== "PRD" || target !== c.env.ENVIRONMENT) return c.json({ error: { code: "registry_target_mismatch" } }, 400);
  if (!c.env.VERSION_BUCKET) return c.json({ error: { code: "version_storage_unavailable" } }, 503);
  try {
    return c.json(await describeClientReleaseRegistry(c.env.VERSION_BUCKET, target, principal.role === "OWNER"));
  } catch {
    return c.json({ error: { code: "version_storage_read_failed" } }, 502);
  }
});
for (const [path, operation] of [
  ["/v1/admin/client-releases/register", "register"],
  ["/v1/admin/client-releases/:versionCode/status", "status"],
  ["/v1/admin/client-releases/:versionCode/promote", "promote"],
  ["/v1/admin/client-releases/minimum", "minimum"],
  ["/v1/admin/client-releases/rollback", "rollback"]
]) {
  app.post(path, async (c) => {
    const principal = await authenticateAdmin(c);
    if (principal instanceof Response) return principal;
    c.header("Cache-Control", "no-store");
    if (principal.role !== "OWNER") return c.json({ error: { code: "registry_mutation_forbidden" } }, 403);
    if (!c.env.VERSION_BUCKET) return c.json({ error: { code: "version_storage_unavailable" } }, 503);
    const body = await c.req.json().catch(() => null);
    if (!body || typeof body !== "object" || Array.isArray(body)) return c.json({ error: { code: "registry_body_invalid" } }, 400);
    try {
      return c.json(await mutateClientReleaseRegistry(
        c.env.VERSION_BUCKET,
        c.env.ENVIRONMENT,
        principal.id,
        operation,
        body,
        c.req.param("versionCode")
      ));
    } catch (error) {
      const code = error instanceof Error ? error.message : "version_storage_write_failed";
      if (code === "version_precondition_required") return c.json({ error: { code } }, 428);
      if (["version_current_pointer_conflict", "version_code_already_registered", "registry_artifact_identity_conflict", "registry_promotion_state_invalid"].includes(code)) return c.json({ error: { code } }, 409);
      if (code.startsWith("registry_") || code === "invalid_version_revision" || code === "client_version_unregistered") return c.json({ error: { code } }, 400);
      return c.json({ error: { code: "version_storage_write_failed" } }, 502);
    }
  });
}
app.get("/v1/admin/server-deployments/current", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  c.header("Cache-Control", "no-store");
  if (!hasAdminPermission(principal, SERVER_DEPLOYMENT_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminServerDeploymentBody(c);
  const service = parseServerDeploymentService(c.req.query("service"));
  const adminEnvironment = currentAdminEnvironment(c);
  const environment = parseServerDeploymentEnvironment(c.req.query("environment"), adminEnvironment);
  if (service === null || environment === null) return c.json({ error: { code: "server_deployment_query_invalid" } }, 400);
  if (adminEnvironment === null || environment !== adminEnvironment) return c.json({ error: { code: "deployment_target_mismatch" } }, 400);
  if (!c.env.VERSION_BUCKET) return c.json({ error: { code: "version_storage_unavailable" } }, 503);
  try {
    const currentKey = `server-deployments/${environment}/${service}/current.json`;
    const object = await c.env.VERSION_BUCKET.get(currentKey);
    if (object === null) {
      return c.json({
        accepted: false,
        environment: c.env.ENVIRONMENT,
        service,
        current: null,
        permission: SERVER_DEPLOYMENT_READ_PERMISSION,
        permissionMode: "ROLE_PERMISSION",
        errorCode: "server_deployment_current_not_found",
        message: "No current server deployment pointer is available."
      }, 404);
    }
    const current = parseServerDeploymentCurrentPointer(await object.json());
    if (current === null) return c.json({ error: { code: "server_deployment_current_invalid" } }, 502);
    if (current.environment !== environment || current.service !== service) {
      return c.json({ error: { code: "server_deployment_current_target_mismatch" } }, 502);
    }
    return c.json({
      accepted: true,
      environment: c.env.ENVIRONMENT,
      service,
      current,
      permission: SERVER_DEPLOYMENT_READ_PERMISSION,
      permissionMode: "ROLE_PERMISSION",
      errorCode: null,
      message: null
    });
  } catch {
    return c.json({ error: { code: "server_deployment_storage_read_failed" } }, 502);
  }
});
app.get("/v1/admin/server-deployments/history/:serverVersion", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  c.header("Cache-Control", "no-store");
  if (!hasAdminPermission(principal, SERVER_DEPLOYMENT_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminServerDeploymentBody(c);
  const service = parseServerDeploymentService(c.req.query("service"));
  const adminEnvironment = currentAdminEnvironment(c);
  const environment = parseServerDeploymentEnvironment(c.req.query("environment"), adminEnvironment);
  const serverVersion = c.req.param("serverVersion");
  if (service === null || environment === null || serverVersion.length === 0) return c.json({ error: { code: "server_deployment_query_invalid" } }, 400);
  if (adminEnvironment === null || environment !== adminEnvironment) return c.json({ error: { code: "deployment_target_mismatch" } }, 400);
  if (!c.env.VERSION_BUCKET) return c.json({ error: { code: "version_storage_unavailable" } }, 503);
  const historyKey = buildServerDeploymentHistoryKey(environment, service, serverVersion);
  try {
    const object = await c.env.VERSION_BUCKET.get(historyKey);
    if (object === null) {
      return c.json({
        accepted: false,
        environment: c.env.ENVIRONMENT,
        service,
        history: null,
        historyKey,
        permission: SERVER_DEPLOYMENT_READ_PERMISSION,
        permissionMode: "ROLE_PERMISSION",
        errorCode: "server_deployment_history_not_found",
        message: "No immutable server deployment history record is available."
      }, 404);
    }
    const history = parseServerDeploymentEvidence(await object.json());
    if (history === null) return c.json({ error: { code: "server_deployment_history_invalid" } }, 502);
    if (history.environment !== environment || history.service !== service || history.serverVersion !== serverVersion) {
      return c.json({ error: { code: "server_deployment_history_target_mismatch" } }, 502);
    }
    return c.json({
      accepted: true,
      environment: c.env.ENVIRONMENT,
      service,
      history,
      historyKey,
      permission: SERVER_DEPLOYMENT_READ_PERMISSION,
      permissionMode: "ROLE_PERMISSION",
      errorCode: null,
      message: null
    });
  } catch {
    return c.json({ error: { code: "server_deployment_storage_read_failed" } }, 502);
  }
});
app.get("/v1/admin/bootstrap/access-subject", async (c) => {
  if (c.env.ENVIRONMENT !== "DEV") {
    return c.json({ error: "NOT_FOUND" }, 404);
  }
  const accessIdentity = await extractAccessIdentity(c);
  if (accessIdentity instanceof Response) return accessIdentity;
  return c.json({
    accessSubject: accessIdentity.subject,
    email: accessIdentity.email
  });
});
app.get("/v1/admin/me", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  return c.json({
    principal: {
      id: principal.id,
      accessSubject: principal.access_subject,
      role: principal.role,
      displayName: principal.display_name
    }
  });
});
app.get("/v1/admin/dashboard/launch-summary", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, SUPPORT_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminDashboardBody(c);
  const now = Date.now();
  const currentKstDayStart = kstCalendarDayStartUtcMs(now);
  const sevenDayStart = currentKstDayStart - 6 * 24 * 60 * 60 * 1e3;
  const thirtyDayStart = currentKstDayStart - 29 * 24 * 60 * 60 * 1e3;
  const sessionSummary = await c.env.DB.prepare(
    `SELECT
       COALESCE(SUM(CASE WHEN status = 'ACTIVE' AND expires_at > ? THEN 1 ELSE 0 END), 0) AS active_session_count,
       MAX(created_at) AS latest_session_created_at
     FROM auth_sessions`
  ).bind(now).first();
  const dailySourceMetrics = await c.env.DB.prepare(
    `SELECT
       (SELECT COUNT(DISTINCT account_id) FROM auth_sessions WHERE created_at >= ?) AS dau_7d,
       (SELECT COUNT(DISTINCT account_id) FROM auth_sessions WHERE created_at >= ?) AS dau_30d,
       (SELECT COUNT(*) FROM accounts WHERE created_at >= ?) AS signups_7d,
       (SELECT COUNT(*) FROM accounts WHERE created_at >= ?) AS signups_30d,
       (SELECT COUNT(*) FROM ad_reward_transactions WHERE created_at >= ? AND status IN ('VERIFIED','GRANTED')) AS ad_rewards_verified_7d,
       (SELECT COUNT(*) FROM ad_reward_transactions WHERE created_at >= ? AND status IN ('VERIFIED','GRANTED')) AS ad_rewards_verified_30d,
       (SELECT COUNT(*) FROM ad_reward_transactions WHERE created_at >= ? AND status = 'GRANTED') AS ad_rewards_granted_7d,
       (SELECT COUNT(*) FROM ad_reward_transactions WHERE created_at >= ? AND status = 'GRANTED') AS ad_rewards_granted_30d`
  ).bind(
    sevenDayStart,
    thirtyDayStart,
    sevenDayStart,
    thirtyDayStart,
    sevenDayStart,
    thirtyDayStart,
    sevenDayStart,
    thirtyDayStart
  ).first();
  const dailySourcePoints = await c.env.DB.prepare(
    `SELECT
       kst_day,
       SUM(session_created_accounts) AS session_created_accounts,
       SUM(signups) AS signups,
       SUM(ad_rewards_verified) AS ad_rewards_verified,
       SUM(ad_rewards_granted) AS ad_rewards_granted
     FROM (
       SELECT
         strftime('%Y-%m-%d', created_at / 1000, 'unixepoch', '+9 hours') AS kst_day,
         COUNT(DISTINCT account_id) AS session_created_accounts,
         0 AS signups,
         0 AS ad_rewards_verified,
         0 AS ad_rewards_granted
       FROM auth_sessions
       WHERE created_at >= ?
       GROUP BY kst_day
       UNION ALL
       SELECT
         strftime('%Y-%m-%d', created_at / 1000, 'unixepoch', '+9 hours') AS kst_day,
         0 AS session_created_accounts,
         COUNT(*) AS signups,
         0 AS ad_rewards_verified,
         0 AS ad_rewards_granted
       FROM accounts
       WHERE created_at >= ?
       GROUP BY kst_day
       UNION ALL
       SELECT
         strftime('%Y-%m-%d', created_at / 1000, 'unixepoch', '+9 hours') AS kst_day,
         0 AS session_created_accounts,
         0 AS signups,
         COUNT(*) AS ad_rewards_verified,
         0 AS ad_rewards_granted
       FROM ad_reward_transactions
       WHERE created_at >= ? AND status IN ('VERIFIED','GRANTED')
       GROUP BY kst_day
       UNION ALL
       SELECT
         strftime('%Y-%m-%d', created_at / 1000, 'unixepoch', '+9 hours') AS kst_day,
         0 AS session_created_accounts,
         0 AS signups,
         0 AS ad_rewards_verified,
         COUNT(*) AS ad_rewards_granted
       FROM ad_reward_transactions
       WHERE created_at >= ? AND status = 'GRANTED'
       GROUP BY kst_day
     )
     GROUP BY kst_day
     ORDER BY kst_day ASC`
  ).bind(thirtyDayStart, thirtyDayStart, thirtyDayStart, thirtyDayStart).all();
  const supportReview = await c.env.DB.prepare(
    `SELECT COUNT(*) AS support_review_needed_count
     FROM suspicious_player_reviews
     WHERE status IN ('REVIEWING','ESCALATED','REWARD_HELD','SCORE_EXCLUDED','ACTIONED')`
  ).bind().first();
  const integrityRisk = await c.env.DB.prepare(
    `SELECT COUNT(DISTINCT account_id) AS integrity_high_risk_count
     FROM integrity_events
     WHERE classification = 'HARD_VIOLATION'
        OR evidence_code IN ('CHECKSUM_MISMATCH','IMPOSSIBLE_SCORE','IMPOSSIBLE_DAMAGE','BALANCE_VERSION_REWARD_MISMATCH')`
  ).bind().first();
  const issueSummary = await c.env.DB.prepare(
    `SELECT
       COALESCE(SUM(CASE WHEN error_code IN ('reward_plan_mismatch','reward_multiplier_missing','reward_plan_unavailable','reward_integrity_restricted') THEN 1 ELSE 0 END), 0) AS reward_claim_failure_count,
       COALESCE(SUM(CASE WHEN error_code IN ('offline_reward_state_conflict','offline_reward_claim_conflict','offline_reward_plan_mismatch','offline_reward_policy_missing','offline_reward_policy_invalid') THEN 1 ELSE 0 END), 0) AS offline_reward_issue_count,
       COALESCE(SUM(CASE WHEN error_code IN ('gacha_state_conflict','gacha_pool_unavailable','gacha_temporarily_unavailable') THEN 1 ELSE 0 END), 0) AS gacha_support_issue_count,
       COALESCE(SUM(CASE WHEN severity = 'critical' AND status = 'open' THEN 1 ELSE 0 END), 0) AS critical_open_ops_error_count,
       COALESCE(SUM(CASE WHEN severity IN ('critical','error') AND status = 'open' THEN 1 ELSE 0 END), 0) AS unacknowledged_core_failure_count
     FROM ops_error_logs
     WHERE status IN ('open','acknowledged')`
  ).bind().first();
  const duplicateReplay = await c.env.DB.prepare(
    `SELECT COUNT(*) AS duplicate_replay_count
     FROM idempotency_records
     WHERE operation IN ('run_start','run_finish','run_reward_claim','offline_reward_claim','login_bonus_claim','gacha_pull')`
  ).bind().first();
  const recoveryCandidates = await c.env.DB.prepare(
    `SELECT COUNT(*) AS account_recovery_candidate_count
     FROM accounts a
     WHERE a.status = 'ACTIVE'
       AND EXISTS (
         SELECT 1 FROM auth_identities guest
         WHERE guest.account_id = a.id AND guest.provider = 'GUEST'
       )
       AND NOT EXISTS (
         SELECT 1 FROM auth_identities google
         WHERE google.account_id = a.id AND google.provider = 'GOOGLE'
       )`
  ).bind().first();
  const storySummary = await c.env.DB.prepare(
    `SELECT
       COUNT(*) AS accounts_with_story_progress_count,
       COALESCE(SUM(CASE WHEN narrative_version IS NOT NULL AND narrative_version != ? THEN 1 ELSE 0 END), 0) AS version_mismatch_count
     FROM account_story_progress`
  ).bind(ACT_ONE_NARRATIVE_COVERAGE_VERSION).first();
  const storyDistribution = await c.env.DB.prepare(
    `SELECT highest_story_progress_id, COUNT(*) AS account_count
     FROM account_story_progress
     GROUP BY highest_story_progress_id
     ORDER BY COUNT(*) DESC, highest_story_progress_id
     LIMIT 20`
  ).bind().all();
  const lastLoginAt = sessionSummary?.latest_session_created_at ?? null;
  const narrativeSummary = actOneNarrativeCoverageSummary(countValue(storySummary?.version_mismatch_count));
  return c.json({
    permission: SUPPORT_READ_PERMISSION,
    permissionMode: "ROLE_PERMISSION",
    activeSessions: countValue(sessionSummary?.active_session_count),
    concurrentUsers: null,
    concurrentUsersPrecision: "UNAVAILABLE",
    recentActiveAccounts5m: null,
    recentActiveAccounts30m: null,
    recentActiveAccountsSource: "not_available_without_activity_source",
    lastLoginAtLatest: toNullableIso(lastLoginAt),
    lastLoginAgeBuckets: {
      latestAgeSeconds: getLastLoginAgeSeconds(lastLoginAt, now),
      exactConcurrentUsersAvailable: false
    },
    dailySourceMetrics: {
      timezone: "Asia/Seoul",
      windowSemantics: "KST_CALENDAR_DAY_INCLUSIVE",
      sessionCreatedAccountsSource: "auth_sessions.created_at_distinct_account",
      dauSource: "UNAVAILABLE_NO_ACTIVITY_HEARTBEAT_SOURCE",
      signupSource: "accounts.created_at",
      adRewardSource: "ad_reward_transactions.status",
      passSource: "UNAVAILABLE_NO_SOURCE_TABLE",
      sourceAvailability: {
        exactDau: false,
        exactConcurrentUsers: false,
        sessionCreatedAccounts: true,
        signups: true,
        adRewards: true,
        passPurchases: false,
        adRevenue: false
      },
      sevenDay: {
        dau: countValue(dailySourceMetrics?.dau_7d),
        sessionCreatedAccounts: countValue(dailySourceMetrics?.dau_7d),
        signups: countValue(dailySourceMetrics?.signups_7d),
        adRewardsVerified: countValue(dailySourceMetrics?.ad_rewards_verified_7d),
        adRewardsGranted: countValue(dailySourceMetrics?.ad_rewards_granted_7d),
        passPurchases: null,
        adRevenue: null
      },
      thirtyDay: {
        dau: countValue(dailySourceMetrics?.dau_30d),
        sessionCreatedAccounts: countValue(dailySourceMetrics?.dau_30d),
        signups: countValue(dailySourceMetrics?.signups_30d),
        adRewardsVerified: countValue(dailySourceMetrics?.ad_rewards_verified_30d),
        adRewardsGranted: countValue(dailySourceMetrics?.ad_rewards_granted_30d),
        passPurchases: null,
        adRevenue: null
      },
      points: dailySourcePoints.results.map((row) => ({
        date: row.kst_day,
        sessionCreatedAccounts: countValue(row.session_created_accounts),
        signups: countValue(row.signups),
        adRewardsVerified: countValue(row.ad_rewards_verified),
        adRewardsGranted: countValue(row.ad_rewards_granted),
        passPurchases: null,
        adRevenue: null
      }))
    },
    supportReviewNeededCount: countValue(supportReview?.support_review_needed_count),
    accountRecoveryCandidateCount: countValue(recoveryCandidates?.account_recovery_candidate_count),
    integrityHighRiskCount: countValue(integrityRisk?.integrity_high_risk_count),
    rewardClaimFailureCount: countValue(issueSummary?.reward_claim_failure_count),
    offlineRewardIssueCount: countValue(issueSummary?.offline_reward_issue_count),
    gachaSupportIssueCount: countValue(issueSummary?.gacha_support_issue_count),
    duplicateReplayCount: countValue(duplicateReplay?.duplicate_replay_count),
    criticalOpenOpsErrorCount: countValue(issueSummary?.critical_open_ops_error_count),
    unacknowledgedCoreFailureCount: countValue(issueSummary?.unacknowledged_core_failure_count),
    accountsWithStoryProgressCount: countValue(storySummary?.accounts_with_story_progress_count),
    highestProgressDistribution: storyDistribution.results.map((row) => ({
      storyProgressId: row.highest_story_progress_id,
      order: storyProgressOrder(row.highest_story_progress_id),
      accountCount: countValue(row.account_count)
    })),
    contentMissingCount: narrativeSummary.contentMissingCount,
    localizationPendingCount: narrativeSummary.localizationPendingCount,
    fixturePendingCount: narrativeSummary.fixturePendingCount,
    versionMismatchCount: narrativeSummary.versionMismatchCount,
    latestNarrativeVersion: narrativeSummary.latestNarrativeVersion,
    serverCalculatedAt: toIso(now),
    supportContext: {
      readOnly: true,
      mutationEndpointsEnabled: false,
      exactConcurrentUsersEnabled: false
    }
  });
});
app.get("/v1/admin/ops/error-logs", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, OPS_ERROR_LOG_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminOpsErrorLogBody(c);
  const whereClauses = [];
  const values = [];
  try {
    const search = opsLogSearch(
      c.req.query("category"),
      c.req.query("q"),
      c.req.query("serverVersion"),
      c.req.query("appVersion"),
      c.req.query("appVersionCode")
    );
    whereClauses.push(...search.clauses);
    values.push(...search.values);
  } catch {
    return c.json({ error: { code: "invalid_log_filter" } }, 400);
  }
  const filters = [
    ["environment", "environment"],
    ["service", "service"],
    ["severity", "severity"],
    ["status", "status"],
    ["source", "source"],
    ["errorCode", "error_code"],
    ["requestId", "request_id"],
    ["accountId", "account_id"],
    ["runId", "run_id"],
    ["rewardPreviewId", "reward_preview_id"],
    ["rewardClaimId", "reward_claim_id"],
    ["offlineRewardClaimId", "offline_reward_claim_id"],
    ["gachaBatchId", "gacha_batch_id"],
    ["gachaBannerId", "gacha_banner_id"],
    ["storyProgressId", "story_progress_id"],
    ["integrityEventId", "integrity_event_id"],
    ["supportTicketId", "support_ticket_id"]
  ];
  for (const [queryName, columnName] of filters) {
    const value = c.req.query(queryName)?.trim();
    if (value !== void 0 && value !== "") {
      whereClauses.push(`${columnName} = ?`);
      values.push(value);
    }
  }
  const occurredFrom = c.req.query("occurredFrom");
  if (occurredFrom !== void 0) {
    const parsed = Date.parse(occurredFrom);
    if (Number.isFinite(parsed)) {
      whereClauses.push("occurred_at >= ?");
      values.push(parsed);
    }
  }
  const occurredTo = c.req.query("occurredTo");
  if (occurredTo !== void 0) {
    const parsed = Date.parse(occurredTo);
    if (Number.isFinite(parsed)) {
      whereClauses.push("occurred_at <= ?");
      values.push(parsed);
    }
  }
  const limitQuery = c.req.query("limit");
  const parsedLimit = limitQuery === void 0 ? 50 : Number(limitQuery);
  const limit = Number.isSafeInteger(parsedLimit) && parsedLimit > 0 ? Math.min(parsedLimit, 50) : 50;
  const cursorQuery = c.req.query("cursor");
  const parsedCursor = cursorQuery === void 0 ? 0 : Number(cursorQuery);
  const offset = Number.isSafeInteger(parsedCursor) && parsedCursor >= 0 ? parsedCursor : 0;
  values.push(limit + 1, offset);
  const rows = await c.env.DB.prepare(
    `SELECT
       id, occurred_at, environment, service, severity, error_code, message_summary,
       status, source, request_id, account_id, run_id, reward_preview_id,
       reward_claim_id, offline_reward_claim_id, gacha_batch_id, gacha_banner_id,
       story_progress_id, integrity_event_id, support_ticket_id,
       idempotency_operation, idempotency_key_hash, correlation_json,
       sanitized_context_json, retryable, masked, payload_ref, trace_ref,
       resolved_at, resolved_by, resolution_note, created_at
     FROM ops_error_logs
     ${whereClauses.length === 0 ? "" : `WHERE ${whereClauses.join(" AND ")}`}
     ORDER BY
       CASE severity
         WHEN 'critical' THEN 0
         WHEN 'error' THEN 1
         WHEN 'warning' THEN 2
         ELSE 3
       END,
       occurred_at DESC,
       id
     LIMIT ? OFFSET ?`
  ).bind(...values).all();
  const visibleRows = rows.results.slice(0, limit);
  return c.json({
    permission: OPS_ERROR_LOG_READ_PERMISSION,
    permissionMode: "ROLE_PERMISSION",
    items: visibleRows.map(opsErrorLogSummary),
    collectionAvailable: true,
    nextCursor: rows.results.length > limit ? String(offset + limit) : null
  });
});
app.get("/v1/admin/ops/error-logs/summary", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, OPS_ERROR_LOG_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminOpsErrorLogBody(c);
  const row = await c.env.DB.prepare(
    `SELECT
       COUNT(*) AS recent_error_count,
       COALESCE(SUM(CASE WHEN severity = 'warning' THEN 1 ELSE 0 END), 0) AS warning_count,
       COALESCE(SUM(CASE WHEN severity = 'critical' THEN 1 ELSE 0 END), 0) AS critical_count,
       COALESCE(SUM(CASE WHEN retryable = 1 THEN 1 ELSE 0 END), 0) AS retryable_count,
       COALESCE(SUM(CASE WHEN masked = 1 THEN 1 ELSE 0 END), 0) AS masked_count,
       COALESCE(SUM(CASE WHEN status = 'open' THEN 1 ELSE 0 END), 0) AS open_count,
       COALESCE(SUM(CASE WHEN status = 'acknowledged' THEN 1 ELSE 0 END), 0) AS acknowledged_count,
       MAX(CASE WHEN severity = 'critical' THEN occurred_at ELSE NULL END) AS latest_critical_at
     FROM ops_error_logs`
  ).bind().first();
  return c.json({
    permission: OPS_ERROR_LOG_READ_PERMISSION,
    permissionMode: "ROLE_PERMISSION",
    recentErrorCount: countValue(row?.recent_error_count),
    warningCount: countValue(row?.warning_count),
    criticalCount: countValue(row?.critical_count),
    retryableCount: countValue(row?.retryable_count),
    maskedCount: countValue(row?.masked_count),
    openCount: countValue(row?.open_count),
    acknowledgedCount: countValue(row?.acknowledged_count),
    latestCriticalAt: toNullableIso(row?.latest_critical_at),
    serverCalculatedAt: toIso(Date.now()),
    detailAvailable: false
  });
});
app.get("/v1/admin/ops/error-logs/:errorLogId", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, OPS_ERROR_LOG_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminOpsErrorLogBody(c);
  const row = await c.env.DB.prepare(
    `SELECT
       id, occurred_at, environment, service, severity, error_code, message_summary,
       status, source, request_id, account_id, run_id, reward_preview_id,
       reward_claim_id, offline_reward_claim_id, gacha_batch_id, gacha_banner_id,
       story_progress_id, integrity_event_id, support_ticket_id,
       idempotency_operation, idempotency_key_hash, correlation_json,
       sanitized_context_json, retryable, masked, payload_ref, trace_ref,
       resolved_at, resolved_by, resolution_note, created_at
     FROM ops_error_logs
     WHERE id = ?`
  ).bind(c.req.param("errorLogId")).first();
  if (row === null) {
    return c.json({ error: "ops_error_log_not_found", message: "Ops error log was not found." }, 404);
  }
  return c.json({
    item: opsErrorLogDetail(row),
    detail: null
  });
});
app.get("/v1/admin/support/tickets", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, SUPPORT_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminSupportBody(c);
  const accountId = c.req.query("accountId")?.trim() || null;
  const category = c.req.query("category")?.trim() || null;
  const status = c.req.query("status")?.trim() || null;
  const limitQuery = c.req.query("limit");
  const parsedLimit = limitQuery === void 0 ? 50 : Number(limitQuery);
  const limit = Number.isSafeInteger(parsedLimit) && parsedLimit > 0 ? Math.min(parsedLimit, 50) : 50;
  const cursorQuery = c.req.query("cursor");
  const parsedCursor = cursorQuery === void 0 ? 0 : Number(cursorQuery);
  const offset = Number.isSafeInteger(parsedCursor) && parsedCursor >= 0 ? parsedCursor : 0;
  const rows = await c.env.DB.prepare(
    `SELECT id, account_id, category, status, body_text, body_preview, body_length,
            app_version, app_version_code, client_composed_at, client_submitted_at,
            server_received_at, idempotency_key, request_hash, created_at, updated_at
     FROM support_tickets
     WHERE (? IS NULL OR account_id = ?)
       AND (? IS NULL OR category = ?)
       AND (? IS NULL OR status = ?)
     ORDER BY server_received_at DESC, id
     LIMIT ? OFFSET ?`
  ).bind(accountId, accountId, category, category, status, status, limit + 1, offset).all();
  const visibleRows = rows.results.slice(0, limit);
  return c.json({
    permission: SUPPORT_READ_PERMISSION,
    permissionMode: "ROLE_PERMISSION",
    items: visibleRows.map(supportTicketSummary),
    nextCursor: rows.results.length > limit ? String(offset + limit) : null,
    supportContext: {
      readOnly: true,
      mutationEndpointsEnabled: false,
      repairEndpointsEnabled: false,
      source: "admin_support_tickets"
    },
    errorCode: null,
    message: null
  });
});
app.get("/v1/admin/support/tickets/:ticketId", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, SUPPORT_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminSupportBody(c);
  const row = await c.env.DB.prepare(
    `SELECT ticket.id, ticket.account_id, ticket.category, ticket.status, ticket.body_text,
            ticket.body_preview, ticket.body_length, ticket.app_version, ticket.app_version_code,
            ticket.client_composed_at, ticket.client_submitted_at, ticket.server_received_at,
            ticket.idempotency_key, ticket.request_hash, ticket.created_at, ticket.updated_at,
            CASE WHEN idem.id IS NULL THEN 0 ELSE 1 END AS idempotency_replay_known
     FROM support_tickets ticket
     LEFT JOIN idempotency_records idem
       ON idem.account_id = ticket.account_id
      AND idem.operation = 'support_ticket_create'
      AND idem.result_ref = ticket.id
     WHERE ticket.id = ?`
  ).bind(c.req.param("ticketId")).first();
  if (row === null) {
    return c.json({ error: "support_ticket_not_found", message: "Support ticket was not found." }, 404);
  }
  return c.json({
    permission: SUPPORT_READ_PERMISSION,
    permissionMode: "ROLE_PERMISSION",
    ticket: supportTicketDetail(row),
    supportContext: {
      readOnly: true,
      mutationEndpointsEnabled: false,
      repairEndpointsEnabled: false,
      source: "admin_support_ticket_detail"
    },
    errorCode: null,
    message: null
  });
});
app.get("/v1/admin/support/accounts/:accountId", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, SUPPORT_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminSupportBody(c);
  const accountId = c.req.param("accountId");
  const account = await c.env.DB.prepare(
    `SELECT
       a.id AS account_id,
       a.status AS account_status,
       a.created_at AS account_created_at,
       a.updated_at AS account_updated_at,
       pp.display_name AS display_name
     FROM accounts a
     LEFT JOIN player_profiles pp ON pp.account_id = a.id
     WHERE a.id = ?`
  ).bind(accountId).first();
  if (account === null) {
    return c.json({ error: "account_not_found", message: "Account was not found." }, 404);
  }
  const identities = await c.env.DB.prepare(
    `SELECT provider
     FROM auth_identities
     WHERE account_id = ?
     ORDER BY provider`
  ).bind(accountId).all();
  const authStatus = await c.env.DB.prepare(
    `SELECT
       COALESCE(SUM(CASE WHEN status = 'ACTIVE' AND expires_at > ? THEN 1 ELSE 0 END), 0) AS active_session_count,
       MAX(created_at) AS latest_session_created_at,
       MAX(CASE WHEN status = 'ACTIVE' AND expires_at > ? THEN expires_at ELSE NULL END) AS latest_session_expires_at,
       MAX(created_at) AS last_login_at
     FROM auth_sessions
     WHERE account_id = ?`
  ).bind(Date.now(), Date.now(), accountId).first();
  const recentRuns = await c.env.DB.prepare(
    `SELECT
       r.id AS run_id,
       r.status AS status,
       r.started_at AS started_at,
       r.finished_at AS finished_at,
       op.risk AS risk,
       r.balance_config_version AS balance_config_version,
       op.preview_json AS preview_json,
       final.outcome AS outcome,
       final.cleared_waves AS cleared_waves,
       preview.id AS reward_preview_id
     FROM runs r
     LEFT JOIN operation_previews op ON op.id = r.preview_id
     LEFT JOIN run_final_results final ON final.run_id = r.id
     LEFT JOIN run_reward_previews preview ON preview.run_id = r.id
     WHERE r.account_id = ?
     ORDER BY r.started_at DESC, r.id
     LIMIT 10`
  ).bind(accountId).all();
  const runRewards = await c.env.DB.prepare(
    `SELECT
       preview.id AS reward_preview_id,
       preview.run_id AS run_id,
       preview.status AS preview_status,
       preview.reward_claim_id AS reward_claim_id,
       preview.claimed_at AS claimed_at,
       preview.balance_config_version AS balance_config_version,
       preview.expires_at AS expires_at,
       preview.reward_plan_json AS reward_plan_json
     FROM run_reward_previews preview
     WHERE preview.account_id = ?
     ORDER BY preview.created_at DESC, preview.id
     LIMIT 10`
  ).bind(accountId).all();
  const offlineRewardRow = await c.env.DB.prepare(
    `SELECT
       a.id AS account_id,
       pp.display_name AS display_name,
       a.status AS account_status,
       a.created_at AS account_created_at,
       sp.highest_story_progress_id AS highest_story_progress_id,
       sp.narrative_version AS narrative_version,
       ors.last_offline_reward_claim_at AS last_offline_reward_claim_at,
       ors.last_story_progress_id AS last_story_progress_id,
       ors.last_balance_config_version AS last_balance_config_version,
       ors.last_narrative_version AS last_narrative_version,
       ors.last_previewed_at AS last_previewed_at,
       ors.revision AS revision,
       ors.updated_at AS state_updated_at,
       last_claim.id AS last_claim_id,
       last_claim.created_at AS last_claim_created_at
     FROM accounts a
     LEFT JOIN player_profiles pp ON pp.account_id = a.id
     LEFT JOIN account_story_progress sp ON sp.account_id = a.id
     LEFT JOIN account_offline_reward_state ors ON ors.account_id = a.id
     LEFT JOIN reward_transactions last_claim
       ON last_claim.id = (
         SELECT rt.id
         FROM reward_transactions rt
         WHERE rt.account_id = a.id AND rt.run_id IS NULL
         ORDER BY rt.created_at DESC, rt.id DESC
         LIMIT 1
       )
     WHERE a.id = ?`
  ).bind(accountId).first();
  const storyRow = await c.env.DB.prepare(
    `SELECT account_id, act_id, highest_cleared_operation_id, highest_story_progress_id,
            cleared_operations_json, story_flags_json, narrative_version,
            last_progress_run_id, updated_at
     FROM account_story_progress
     WHERE account_id = ?`
  ).bind(accountId).first();
  const unlockRows = await c.env.DB.prepare(
    `SELECT unlock_id
     FROM account_unlocks
     WHERE account_id = ? AND unlock_id LIKE 'operation:%'
     ORDER BY unlocked_at, unlock_id`
  ).bind(accountId).all();
  const gachaBatches = await c.env.DB.prepare(
    `SELECT
       id AS batch_id,
       banner_id AS banner_id,
       pull_count AS pull_count,
       cost_item_id AS cost_item_id,
       cost_quantity AS cost_quantity,
       result_summary_json AS result_summary_json,
       created_at AS created_at
     FROM gacha_pull_batches
     WHERE account_id = ?
     ORDER BY created_at DESC, id
     LIMIT 10`
  ).bind(accountId).all();
  const idempotencyRows = await c.env.DB.prepare(
    `SELECT operation, result_ref, created_at
     FROM idempotency_records
     WHERE account_id = ?
       AND operation IN ('run_finish','run_reward_claim','offline_reward_claim','login_bonus_claim','gacha_pull')
     ORDER BY created_at DESC, id
     LIMIT 20`
  ).bind(accountId).all();
  const integrity = await c.env.DB.prepare(
    `SELECT
       COUNT(event.id) AS open_signals,
       COALESCE(SUM(CASE WHEN event.classification = 'HARD_VIOLATION' THEN 1 ELSE 0 END), 0) AS hard_violations,
       MAX(event.created_at) AS latest_detection_at,
       (
         SELECT latest.evidence_code
         FROM integrity_events latest
         WHERE latest.account_id = ?
         ORDER BY latest.created_at DESC, latest.id DESC
         LIMIT 1
       ) AS primary_reason
     FROM integrity_events event
     LEFT JOIN integrity_event_review_states review ON review.event_id = event.id
     WHERE event.account_id = ?
       AND COALESCE(review.state, 'OPEN') != 'RESOLVED'`
  ).bind(accountId, accountId).first();
  const suspiciousReview = await c.env.DB.prepare(
    `SELECT status, reason, updated_at
     FROM suspicious_player_reviews
     WHERE account_id = ?`
  ).bind(accountId).first();
  const opsErrors = await c.env.DB.prepare(
    `SELECT
       id, occurred_at, environment, service, severity, error_code, message_summary,
       status, source, request_id, account_id, run_id, reward_preview_id,
       reward_claim_id, offline_reward_claim_id, gacha_batch_id, gacha_banner_id,
       story_progress_id, integrity_event_id, support_ticket_id,
       idempotency_operation, idempotency_key_hash, correlation_json,
       sanitized_context_json, retryable, masked, payload_ref, trace_ref,
       resolved_at, resolved_by, resolution_note, created_at
     FROM ops_error_logs
     WHERE account_id = ?
       AND error_code IN (
         'google_auth_config_missing',
         'offline_reward_state_conflict',
         'gacha_state_conflict',
         'story_progress_state_conflict'
       )
       AND status IN ('open','acknowledged')
     ORDER BY
       CASE severity
         WHEN 'critical' THEN 0
         WHEN 'error' THEN 1
         WHEN 'warning' THEN 2
         ELSE 3
       END,
       occurred_at DESC,
       id
     LIMIT 5`
  ).bind(accountId).all();
  const loginBonusClaim = await c.env.DB.prepare(
    `SELECT bonus_id, reward_transaction_id, claimed_at, reward_json
     FROM account_login_bonus_claims
     WHERE account_id = ? AND bonus_id = ?`
  ).bind(accountId, LAUNCH_LOGIN_BONUS_ID).first();
  const providers = identities.results.map((row) => row.provider);
  const lastLoginAt = authStatus?.last_login_at ?? null;
  return c.json({
    accountId: account.account_id,
    generatedAt: (/* @__PURE__ */ new Date()).toISOString(),
    permission: SUPPORT_READ_PERMISSION,
    permissionMode: "ROLE_PERMISSION",
    account: {
      accountId: account.account_id,
      accountStatus: account.account_status,
      displayName: account.display_name,
      createdAt: toIso(account.account_created_at),
      updatedAt: toIso(account.account_updated_at)
    },
    auth: {
      identityProviders: providers,
      providerNames: providers,
      linkedProviders: providers,
      primaryProvider: getSupportPrimaryProvider(providers),
      hasGoogleLink: providers.includes("GOOGLE"),
      activeSessionCount: authStatus?.active_session_count ?? 0,
      latestSessionCreatedAt: toNullableIso(authStatus?.latest_session_created_at),
      latestSessionAt: toNullableIso(authStatus?.latest_session_created_at),
      latestSessionExpiresAt: toNullableIso(authStatus?.latest_session_expires_at),
      latestAuthAt: toNullableIso(lastLoginAt),
      lastLoginAt: toNullableIso(lastLoginAt),
      lastLoginAgeSeconds: getLastLoginAgeSeconds(lastLoginAt, Date.now()),
      lastSeenAt: null,
      lastSeenSource: "not_available_without_activity_source",
      concurrentUsers: null,
      concurrentUsersPrecision: "UNAVAILABLE"
    },
    recentRuns: recentRuns.results.map((run) => {
      const previewMetadata = parseRunPreviewMetadata(run.preview_json);
      return {
        runId: run.run_id,
        status: run.status,
        operationId: previewMetadata.operationId,
        actId: previewMetadata.actId,
        regionId: previewMetadata.regionId,
        encounterType: previewMetadata.encounterType,
        raidId: previewMetadata.raidId,
        bossId: previewMetadata.bossId,
        bossPhaseRefs: previewMetadata.bossPhaseRefs,
        allowedOperatorSlots: previewMetadata.allowedOperatorSlots,
        startAugmentPackageRef: previewMetadata.startAugmentPackageRef,
        startTacticPackageRef: previewMetadata.startTacticPackageRef,
        contentVersion: previewMetadata.contentVersion,
        risk: normalizeRiskTier(run.risk),
        balanceVersion: String(run.balance_config_version),
        startedAt: toIso(run.started_at),
        finishedAt: run.finished_at === null ? null : toIso(run.finished_at),
        outcome: run.outcome,
        clearedWaves: run.cleared_waves,
        rewardPreviewId: run.reward_preview_id
      };
    }),
    runRewards: runRewards.results.map((reward) => ({
      rewardPreviewId: reward.reward_preview_id,
      runId: reward.run_id,
      previewStatus: reward.preview_status,
      claimability: reward.preview_status === "CLAIMED" ? "ALREADY_CLAIMED" : reward.preview_status,
      rewardClaimId: reward.reward_claim_id,
      claimedAt: reward.claimed_at === null ? null : toIso(reward.claimed_at),
      balanceVersion: String(reward.balance_config_version),
      expiresAt: toIso(reward.expires_at),
      rewardLineSummary: parseRewardLineSummary(reward.reward_plan_json),
      idempotencyReplayKnown: idempotencyRows.results.some((row) => row.operation === "run_reward_claim" && row.result_ref === reward.reward_claim_id)
    })),
    offlineRewards: offlineRewardSummary(offlineRewardRow ?? supportOfflineRewardFallback(account)),
    storyProgress: storyRow === null ? defaultStoryProgressDetail(accountId) : storyProgressDetail(storyRow, unlockRows.results),
    gacha: {
      recentBatches: gachaBatches.results.map((batch) => ({
        batchId: batch.batch_id,
        bannerId: batch.banner_id,
        pullCount: batch.pull_count,
        cost: {
          itemId: batch.cost_item_id,
          quantity: batch.cost_quantity
        },
        guaranteeTypes: parseGachaGuaranteeTypes(batch.result_summary_json),
        postPityState: parseGachaPostPityState(batch.result_summary_json),
        createdAt: toIso(batch.created_at)
      }))
    },
    loginBonus: supportLoginBonusSummary(loginBonusClaim, idempotencyRows.results),
    idempotency: idempotencyRows.results.map((row) => ({
      operation: row.operation,
      resultRef: row.result_ref,
      createdAt: toIso(row.created_at),
      replaySafe: true,
      linkedResourceType: row.operation === "gacha_pull" ? "gacha_batch" : row.operation === "login_bonus_claim" ? "login_bonus_claim" : row.operation === "offline_reward_claim" || row.operation === "run_reward_claim" ? "reward_transaction" : "reward_preview"
    })),
    integrity: {
      openSignals: integrity?.open_signals ?? 0,
      hardViolations: integrity?.hard_violations ?? 0,
      latestDetectionAt: integrity?.latest_detection_at === null || integrity?.latest_detection_at === void 0 ? null : toIso(integrity.latest_detection_at),
      accountReviewStatus: suspiciousReview === null ? "NEW" : suspiciousReviewStatus(suspiciousReview.status),
      reviewReason: suspiciousReview?.reason ?? null,
      reviewUpdatedAt: suspiciousReview === null ? null : toIso(suspiciousReview.updated_at),
      primaryReason: integrity?.primary_reason ?? null
    },
    opsErrors: opsErrors.results.map(supportOpsErrorSummary),
    supportNotes: supportExplanationTemplates(),
    supportContext: {
      readOnly: true,
      mutationEndpointsEnabled: false,
      repairEndpointsEnabled: false,
      source: "support_account_summary"
    },
    errorCode: null,
    message: null
  });
});
app.get("/v1/admin/gacha/banners", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, SUPPORT_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminGachaBody(c);
  const status = c.req.query("status")?.trim() || null;
  const rows = await c.env.DB.prepare(
    `SELECT
       id, banner_type, status, balance_config_version, pickup_operator_id,
       starts_at, ends_at, pity_config_json, pool_snapshot_json, created_at, updated_at
     FROM gacha_banners
     WHERE (? IS NULL OR status = ?)
     ORDER BY
       CASE status WHEN 'ACTIVE' THEN 0 ELSE 1 END,
       updated_at DESC,
       id
     LIMIT 50`
  ).bind(status, status).all();
  const items = rows.results.map(adminGachaBannerSummary);
  return c.json({
    permission: SUPPORT_READ_PERMISSION,
    permissionMode: "ROLE_PERMISSION",
    activeBannerId: items.find((item) => item.status === "ACTIVE")?.bannerId ?? null,
    items,
    nextCursor: null
  });
});
app.get("/v1/admin/gacha/accounts/:accountId/pity", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, SUPPORT_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminGachaBody(c);
  const accountId = c.req.param("accountId");
  const bannerId = c.req.query("bannerId")?.trim() || null;
  const row = await c.env.DB.prepare(
    `SELECT
       account_id, banner_id, total_pull_count, elite_pity_count,
       pickup_elite_pity_count, rare_or_higher_pity_count,
       revision, last_batch_id, updated_at
     FROM player_gacha_pity
     WHERE account_id = ?
       AND (? IS NULL OR banner_id = ?)
     ORDER BY updated_at DESC, banner_id
     LIMIT 1`
  ).bind(accountId, bannerId, bannerId).first();
  return c.json({
    permission: SUPPORT_READ_PERMISSION,
    permissionMode: "ROLE_PERMISSION",
    ...adminGachaPitySummary(row, accountId, bannerId ?? row?.banner_id ?? null)
  });
});
app.get("/v1/admin/gacha/pull-batches", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, SUPPORT_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminGachaBody(c);
  const accountId = c.req.query("accountId")?.trim() || null;
  const bannerId = c.req.query("bannerId")?.trim() || null;
  const limitQuery = c.req.query("limit");
  const parsedLimit = limitQuery === void 0 ? 50 : Number(limitQuery);
  const limit = Number.isSafeInteger(parsedLimit) && parsedLimit > 0 ? Math.min(parsedLimit, 50) : 50;
  const cursorQuery = c.req.query("cursor");
  const parsedCursor = cursorQuery === void 0 ? 0 : Number(cursorQuery);
  const offset = Number.isSafeInteger(parsedCursor) && parsedCursor >= 0 ? parsedCursor : 0;
  const rows = await c.env.DB.prepare(
    `SELECT
       batch.id AS batch_id,
       batch.account_id AS account_id,
       batch.banner_id AS banner_id,
       batch.pull_count AS pull_count,
       batch.cost_item_id AS cost_item_id,
       batch.cost_quantity AS cost_quantity,
       batch.cost_ledger_id AS cost_ledger_id,
       batch.pre_pity_json AS pre_pity_json,
       batch.post_pity_json AS post_pity_json,
       batch.result_summary_json AS result_summary_json,
       batch.created_at AS created_at,
       CASE WHEN idem.id IS NULL THEN 0 ELSE 1 END AS idempotency_replay_known
     FROM gacha_pull_batches batch
     LEFT JOIN idempotency_records idem
       ON idem.account_id = batch.account_id
      AND idem.operation = 'gacha_pull'
      AND idem.result_ref = batch.id
     WHERE (? IS NULL OR batch.account_id = ?)
       AND (? IS NULL OR batch.banner_id = ?)
     ORDER BY batch.created_at DESC, batch.id
     LIMIT ? OFFSET ?`
  ).bind(accountId, accountId, bannerId, bannerId, limit + 1, offset).all();
  const visibleRows = rows.results.slice(0, limit);
  return c.json({
    permission: SUPPORT_READ_PERMISSION,
    permissionMode: "ROLE_PERMISSION",
    items: visibleRows.map(adminGachaBatchSummary),
    nextCursor: rows.results.length > limit ? String(offset + limit) : null
  });
});
app.get("/v1/admin/gacha/pull-batches/:batchId", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, SUPPORT_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminGachaBody(c);
  const batchId = c.req.param("batchId");
  const batch = await c.env.DB.prepare(
    `SELECT
       batch.id AS batch_id,
       batch.account_id AS account_id,
       batch.banner_id AS banner_id,
       batch.pull_count AS pull_count,
       batch.cost_item_id AS cost_item_id,
       batch.cost_quantity AS cost_quantity,
       batch.cost_ledger_id AS cost_ledger_id,
       batch.pre_pity_json AS pre_pity_json,
       batch.post_pity_json AS post_pity_json,
       batch.result_summary_json AS result_summary_json,
       batch.created_at AS created_at,
       CASE WHEN idem.id IS NULL THEN 0 ELSE 1 END AS idempotency_replay_known
     FROM gacha_pull_batches batch
     LEFT JOIN idempotency_records idem
       ON idem.account_id = batch.account_id
      AND idem.operation = 'gacha_pull'
      AND idem.result_ref = batch.id
     WHERE batch.id = ?`
  ).bind(batchId).first();
  if (batch === null) {
    return c.json({ error: "gacha_batch_not_found", message: "Gacha pull batch was not found." }, 404);
  }
  const results = await c.env.DB.prepare(
    `SELECT
       id AS result_id,
       pull_sequence AS pull_sequence,
       result_rarity AS result_rarity,
       result_type AS result_type,
       result_id AS reward_id,
       is_pickup AS is_pickup,
       was_duplicate AS was_duplicate,
       guarantee_type AS guarantee_type,
       duplicate_conversion_json AS duplicate_conversion_json,
       item_ledger_id AS item_ledger_id,
       progression_ledger_id AS progression_ledger_id
     FROM gacha_pull_results
     WHERE batch_id = ?
     ORDER BY pull_sequence ASC`
  ).bind(batchId).all();
  const itemLedgerRefs = [
    batch.cost_ledger_id ?? null,
    ...results.results.map((result) => result.item_ledger_id)
  ].filter((value) => value !== null);
  const operatorLedgerRefs = results.results.map((result) => result.progression_ledger_id).filter((value) => value !== null);
  const storedResponseResults = parseGachaResponseResults(batch.result_summary_json);
  const resultItems = results.results.map((row) => {
    const stored = storedResponseResults.find((value) => isJsonObject(value) && value.pullSequence === row.pull_sequence && value.resultId === row.reward_id && value.resultRarity === row.result_rarity && value.resultType === row.result_type);
    const reward = isJsonObject(stored) && isJsonObject(stored.reward) ? stored.reward : null;
    const quantity = reward && typeof reward.quantity === "number" && Number.isSafeInteger(reward.quantity) && reward.quantity > 0 ? reward.quantity : parseDuplicateConversion(row.duplicate_conversion_json)?.quantity ?? null;
    return adminGachaResultSummary(row, quantity);
  });
  return c.json({
    item: {
      ...adminGachaBatchSummary(batch),
      itemLedgerRefs,
      operatorLedgerRefs,
      results: resultItems,
      causeEvidence: adminGachaBatchCauseEvidence(batch, resultItems, itemLedgerRefs, operatorLedgerRefs)
    },
    detailAvailable: true
  });
});
app.get("/v1/admin/offline-rewards", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, OFFLINE_REWARD_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminOfflineRewardBody(c);
  const whereClauses = [];
  const values = [];
  const filters = [
    ["accountId", "a.id"],
    ["accountStatus", "a.status"],
    ["storyProgressId", "COALESCE(sp.highest_story_progress_id, ors.last_story_progress_id, 'none')"],
    ["lastStoryProgressId", "COALESCE(ors.last_story_progress_id, 'none')"],
    ["narrativeVersion", "COALESCE(sp.narrative_version, ors.last_narrative_version, ?)"]
  ];
  for (const [queryName, columnName] of filters) {
    const value = c.req.query(queryName)?.trim();
    if (value !== void 0 && value !== "") {
      whereClauses.push(`${columnName} = ?`);
      if (queryName === "narrativeVersion") values.push(ACT_ONE_NARRATIVE_VERSION);
      values.push(value);
    }
  }
  const updatedFrom = c.req.query("updatedFrom");
  if (updatedFrom !== void 0) {
    const parsed = Date.parse(updatedFrom);
    if (Number.isFinite(parsed)) {
      whereClauses.push("COALESCE(ors.updated_at, a.updated_at) >= ?");
      values.push(parsed);
    }
  }
  const updatedTo = c.req.query("updatedTo");
  if (updatedTo !== void 0) {
    const parsed = Date.parse(updatedTo);
    if (Number.isFinite(parsed)) {
      whereClauses.push("COALESCE(ors.updated_at, a.updated_at) <= ?");
      values.push(parsed);
    }
  }
  const limitQuery = c.req.query("limit");
  const parsedLimit = limitQuery === void 0 ? 50 : Number(limitQuery);
  const limit = Number.isSafeInteger(parsedLimit) && parsedLimit > 0 ? Math.min(parsedLimit, 50) : 50;
  const cursorQuery = c.req.query("cursor");
  const parsedCursor = cursorQuery === void 0 ? 0 : Number(cursorQuery);
  const offset = Number.isSafeInteger(parsedCursor) && parsedCursor >= 0 ? parsedCursor : 0;
  values.push(limit + 1, offset);
  const rows = await c.env.DB.prepare(
    `SELECT
       a.id AS account_id,
       pp.display_name AS display_name,
       a.status AS account_status,
       a.created_at AS account_created_at,
       sp.highest_story_progress_id AS highest_story_progress_id,
       sp.narrative_version AS narrative_version,
       ors.last_offline_reward_claim_at AS last_offline_reward_claim_at,
       ors.last_story_progress_id AS last_story_progress_id,
       ors.last_balance_config_version AS last_balance_config_version,
       ors.last_narrative_version AS last_narrative_version,
       ors.last_previewed_at AS last_previewed_at,
       ors.revision AS revision,
       ors.updated_at AS state_updated_at,
       last_claim.id AS last_claim_id,
       last_claim.created_at AS last_claim_created_at
     FROM accounts a
     LEFT JOIN player_profiles pp ON pp.account_id = a.id
     LEFT JOIN account_story_progress sp ON sp.account_id = a.id
     LEFT JOIN account_offline_reward_state ors ON ors.account_id = a.id
     LEFT JOIN reward_transactions last_claim
       ON last_claim.id = (
         SELECT rt.id
         FROM reward_transactions rt
         WHERE rt.account_id = a.id AND rt.run_id IS NULL
         ORDER BY rt.created_at DESC, rt.id DESC
         LIMIT 1
       )
     ${whereClauses.length === 0 ? "" : `WHERE ${whereClauses.join(" AND ")}`}
     ORDER BY COALESCE(ors.updated_at, a.updated_at) DESC, a.id
     LIMIT ? OFFSET ?`
  ).bind(...values).all();
  const visibleRows = rows.results.slice(0, limit);
  return c.json({
    permission: OFFLINE_REWARD_READ_PERMISSION,
    permissionMode: "ROLE_PERMISSION",
    items: visibleRows.map(offlineRewardSummary),
    nextCursor: rows.results.length > limit ? String(offset + limit) : null
  });
});
app.get("/v1/admin/offline-rewards/:accountId", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, OFFLINE_REWARD_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminOfflineRewardBody(c);
  const accountId = c.req.param("accountId");
  const row = await c.env.DB.prepare(
    `SELECT
       a.id AS account_id,
       pp.display_name AS display_name,
       a.status AS account_status,
       a.created_at AS account_created_at,
       sp.highest_story_progress_id AS highest_story_progress_id,
       sp.narrative_version AS narrative_version,
       ors.last_offline_reward_claim_at AS last_offline_reward_claim_at,
       ors.last_story_progress_id AS last_story_progress_id,
       ors.last_balance_config_version AS last_balance_config_version,
       ors.last_narrative_version AS last_narrative_version,
       ors.last_previewed_at AS last_previewed_at,
       ors.revision AS revision,
       ors.updated_at AS state_updated_at,
       last_claim.id AS last_claim_id,
       last_claim.created_at AS last_claim_created_at
     FROM accounts a
     LEFT JOIN player_profiles pp ON pp.account_id = a.id
     LEFT JOIN account_story_progress sp ON sp.account_id = a.id
     LEFT JOIN account_offline_reward_state ors ON ors.account_id = a.id
     LEFT JOIN reward_transactions last_claim
       ON last_claim.id = (
         SELECT rt.id
         FROM reward_transactions rt
         WHERE rt.account_id = a.id AND rt.run_id IS NULL
         ORDER BY rt.created_at DESC, rt.id DESC
         LIMIT 1
       )
     WHERE a.id = ?`
  ).bind(accountId).first();
  if (row === null) {
    return c.json({ error: "account_not_found", message: "Account was not found." }, 404);
  }
  return c.json(offlineRewardSummary(row));
});
app.get("/v1/admin/narrative/coverage", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, STORY_PROGRESS_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminStoryProgressBody(c);
  const actId = c.req.query("actId")?.trim() || "act_01_wasteland_railway";
  if (actId !== "act_01_wasteland_railway") {
    return c.json({
      permission: STORY_PROGRESS_READ_PERMISSION,
      permissionMode: "ROLE_PERMISSION",
      actId,
      narrativeVersion: null,
      items: [],
      summary: emptyNarrativeCoverageSummary(),
      supportContext: {
        readOnly: true,
        mutationEndpointsEnabled: false,
        narrativeEditingEnabled: false,
        localizationPublishingEnabled: false,
        source: "narrative_coverage_static_candidates"
      }
    });
  }
  return c.json({
    permission: STORY_PROGRESS_READ_PERMISSION,
    permissionMode: "ROLE_PERMISSION",
    actId,
    narrativeVersion: ACT_ONE_NARRATIVE_COVERAGE_VERSION,
    items: actOneNarrativeCoverageItems(),
    summary: actOneNarrativeCoverageSummary(),
    supportContext: {
      readOnly: true,
      mutationEndpointsEnabled: false,
      narrativeEditingEnabled: false,
      localizationPublishingEnabled: false,
      source: "narrative_coverage_static_candidates"
    }
  });
});
app.get("/v1/admin/story-progress", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, STORY_PROGRESS_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminStoryProgressBody(c);
  const constantFilters = [
    ["contentStatus", "CANDIDATE"],
    ["localizationStatus", "PENDING"],
    ["fixtureStatus", "PARTIAL"],
    ["missingContentStatus", "UNKNOWN"]
  ];
  if (constantFilters.some(([queryName, expected]) => {
    const actual = c.req.query(queryName);
    return actual !== void 0 && actual !== expected;
  })) {
    return c.json({
      permission: STORY_PROGRESS_READ_PERMISSION,
      permissionMode: "ROLE_PERMISSION",
      items: [],
      nextCursor: null
    });
  }
  const whereClauses = [];
  const values = [];
  const filters = [
    ["accountId", "account_id"],
    ["actId", "act_id"],
    ["highestStoryProgressId", "highest_story_progress_id"],
    ["highestClearedOperationId", "highest_cleared_operation_id"],
    ["narrativeVersion", "narrative_version"]
  ];
  for (const [queryName, columnName] of filters) {
    const value = c.req.query(queryName)?.trim();
    if (value !== void 0 && value !== "") {
      whereClauses.push(`${columnName} = ?`);
      values.push(value);
    }
  }
  const updatedFrom = c.req.query("updatedFrom");
  if (updatedFrom !== void 0) {
    const parsed = Date.parse(updatedFrom);
    if (Number.isFinite(parsed)) {
      whereClauses.push("updated_at >= ?");
      values.push(parsed);
    }
  }
  const updatedTo = c.req.query("updatedTo");
  if (updatedTo !== void 0) {
    const parsed = Date.parse(updatedTo);
    if (Number.isFinite(parsed)) {
      whereClauses.push("updated_at <= ?");
      values.push(parsed);
    }
  }
  const limitQuery = c.req.query("limit");
  const parsedLimit = limitQuery === void 0 ? 50 : Number(limitQuery);
  const limit = Number.isSafeInteger(parsedLimit) && parsedLimit > 0 ? Math.min(parsedLimit, 50) : 50;
  const cursorQuery = c.req.query("cursor");
  const parsedCursor = cursorQuery === void 0 ? 0 : Number(cursorQuery);
  const offset = Number.isSafeInteger(parsedCursor) && parsedCursor >= 0 ? parsedCursor : 0;
  values.push(limit + 1, offset);
  const rows = await c.env.DB.prepare(
    `SELECT account_id, act_id, highest_cleared_operation_id, highest_story_progress_id,
            cleared_operations_json, story_flags_json, narrative_version,
            last_progress_run_id, updated_at
     FROM account_story_progress
     ${whereClauses.length === 0 ? "" : `WHERE ${whereClauses.join(" AND ")}`}
     ORDER BY updated_at DESC, account_id
     LIMIT ? OFFSET ?`
  ).bind(...values).all();
  const visibleRows = rows.results.slice(0, limit);
  return c.json({
    permission: STORY_PROGRESS_READ_PERMISSION,
    permissionMode: "ROLE_PERMISSION",
    items: visibleRows.map(storyProgressSummary),
    nextCursor: rows.results.length > limit ? String(offset + limit) : null
  });
});
app.get("/v1/admin/story-progress/:accountId", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (!hasAdminPermission(principal, STORY_PROGRESS_READ_PERMISSION)) return adminPermissionDenied();
  if (hasRequestBody(c)) return rejectAdminStoryProgressBody(c);
  const accountId = c.req.param("accountId");
  const row = await c.env.DB.prepare(
    `SELECT account_id, act_id, highest_cleared_operation_id, highest_story_progress_id,
            cleared_operations_json, story_flags_json, narrative_version,
            last_progress_run_id, updated_at
     FROM account_story_progress
     WHERE account_id = ?`
  ).bind(accountId).first();
  if (row === null) {
    return c.json(defaultStoryProgressDetail(accountId));
  }
  const unlockRows = await c.env.DB.prepare(
    `SELECT unlock_id
     FROM account_unlocks
     WHERE account_id = ? AND unlock_id LIKE 'operation:%'
     ORDER BY unlocked_at, unlock_id`
  ).bind(accountId).all();
  return c.json(storyProgressDetail(row, unlockRows.results));
});
app.get("/v1/admin/players", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  const rawQuery = c.req.query("q") ?? "";
  const query = rawQuery.trim();
  const likeQuery = `%${query}%`;
  const rows = await c.env.DB.prepare(
    `SELECT
       a.id AS account_id,
       pp.display_name AS display_name,
       a.status AS status,
       a.updated_at AS updated_at,
       CASE
         WHEN (SELECT COUNT(*) FROM operator_training_core_slots core WHERE core.account_id = a.id) = 5
         THEN (SELECT MIN(trained_level) FROM operator_training_core_slots core WHERE core.account_id = a.id)
         ELSE 1
       END AS sync_level
     FROM accounts a
     LEFT JOIN player_profiles pp ON pp.account_id = a.id
     WHERE (? = '' OR a.id LIKE ? OR pp.display_name LIKE ?)
     ORDER BY a.updated_at DESC
     LIMIT 50`
  ).bind(query, likeQuery, likeQuery).all();
  return c.json({
    items: rows.results.map((row) => ({
      accountId: row.account_id,
      displayName: row.display_name ?? row.account_id,
      status: row.status,
      syncLevel: row.sync_level ?? 1,
      lastSeenAt: toIso(row.updated_at)
    }))
  });
});
app.get("/v1/admin/players/:accountId", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (hasRequestBody(c)) return rejectAdminPlayerBody(c);
  const accountId = c.req.param("accountId");
  const account = await c.env.DB.prepare(
    `SELECT
       a.id AS account_id,
       pp.display_name AS display_name,
       a.status AS status,
       a.created_at AS created_at,
       ac.balance AS operator_exp
     FROM accounts a
     LEFT JOIN player_profiles pp ON pp.account_id = a.id
     LEFT JOIN account_currencies ac
       ON ac.account_id = a.id
      AND ac.currency = 'OPERATOR_EXP'
     WHERE a.id = ?`
  ).bind(accountId).first();
  if (account === null) {
    return c.json({ error: "ADMIN_PLAYER_NOT_FOUND" }, 404);
  }
  const core = await c.env.DB.prepare(
    `SELECT slot_index, operator_id, trained_level
     FROM operator_training_core_slots
     WHERE account_id = ?
     ORDER BY slot_index ASC`
  ).bind(accountId).all();
  const operators = await c.env.DB.prepare(
    `SELECT operator_id, skill_training, promotion
     FROM account_operators
     WHERE account_id = ?
     ORDER BY operator_id ASC`
  ).bind(accountId).all();
  const identities = await c.env.DB.prepare(
    `SELECT provider
     FROM auth_identities
     WHERE account_id = ?
     ORDER BY provider`
  ).bind(accountId).all();
  const now = Date.now();
  const authStatus = await c.env.DB.prepare(
    `SELECT
       COALESCE(SUM(CASE WHEN status = 'ACTIVE' AND expires_at > ? THEN 1 ELSE 0 END), 0) AS active_session_count,
       MAX(created_at) AS latest_session_created_at,
       MAX(CASE WHEN status = 'ACTIVE' AND expires_at > ? THEN expires_at ELSE NULL END) AS latest_session_expires_at,
       MAX(created_at) AS last_login_at
     FROM auth_sessions
     WHERE account_id = ?`
  ).bind(now, now, accountId).first();
  const opsErrors = await c.env.DB.prepare(
    `SELECT
       id, occurred_at, environment, service, severity, error_code, message_summary,
       status, source, request_id, account_id, run_id, reward_preview_id,
       reward_claim_id, offline_reward_claim_id, gacha_batch_id, gacha_banner_id,
       story_progress_id, integrity_event_id, support_ticket_id,
       idempotency_operation, idempotency_key_hash, correlation_json,
       sanitized_context_json, retryable, masked, payload_ref, trace_ref,
       resolved_at, resolved_by, resolution_note, created_at
     FROM ops_error_logs
     WHERE account_id = ?
       AND error_code IN (
         'google_auth_config_missing',
         'offline_reward_state_conflict',
         'gacha_state_conflict',
         'story_progress_state_conflict'
       )
       AND status IN ('open','acknowledged')
     ORDER BY
       CASE severity
         WHEN 'critical' THEN 0
         WHEN 'error' THEN 1
         WHEN 'warning' THEN 2
         ELSE 3
       END,
       occurred_at DESC,
       id
     LIMIT 5`
  ).bind(accountId).all();
  const coreRows = core.results;
  const syncLevel = syncLevelFrom(coreRows);
  const coreByOperator = new Map(coreRows.map((row) => [row.operator_id, row]));
  const providers = validSupportIdentityRows(identities.results).map((row) => row.provider);
  const lastLoginAt = authStatus?.last_login_at ?? null;
  return c.json({
    accountId: account.account_id,
    displayName: account.display_name ?? account.account_id,
    status: account.status,
    createdAt: toIso(account.created_at),
    progression: {
      operatorExp: account.operator_exp ?? 0,
      syncLevel,
      coreLevelSpread: coreLevelSpreadFrom(coreRows),
      trainingCore: coreRows.map((row) => ({
        slot: row.slot_index + 1,
        operatorId: row.operator_id,
        name: row.operator_id,
        trainedLevel: row.trained_level
      })),
      operators: operators.results.map((operator) => {
        const coreSlot = coreByOperator.get(operator.operator_id);
        const isTrainingCore = coreSlot !== void 0;
        return {
          operatorId: operator.operator_id,
          name: operator.operator_id,
          effectiveLevel: isTrainingCore ? coreSlot.trained_level : syncLevel,
          levelSource: isTrainingCore ? "TRAINED" : "SYNC",
          trainedLevel: coreSlot?.trained_level ?? 1,
          skillTraining: operator.skill_training,
          promotion: String(operator.promotion),
          ...coreSlot ? { coreSlot: coreSlot.slot_index + 1 } : {}
        };
      })
    },
    auth: {
      identityProviders: providers,
      providerNames: providers,
      linkedProviders: providers,
      primaryProvider: getSupportPrimaryProvider(providers),
      hasGoogleLink: providers.includes("GOOGLE"),
      activeSessionCount: authStatus?.active_session_count ?? 0,
      latestSessionCreatedAt: toNullableIso(authStatus?.latest_session_created_at),
      latestSessionAt: toNullableIso(authStatus?.latest_session_created_at),
      latestSessionExpiresAt: toNullableIso(authStatus?.latest_session_expires_at),
      latestAuthAt: toNullableIso(lastLoginAt),
      lastLoginAt: toNullableIso(lastLoginAt),
      lastLoginAgeSeconds: getLastLoginAgeSeconds(lastLoginAt, now),
      lastSeenAt: null,
      lastSeenSource: "not_available_without_activity_source",
      concurrentUsers: null,
      concurrentUsersPrecision: "UNAVAILABLE"
    },
    opsErrors: validOpsErrorRows(opsErrors.results).map(supportOpsErrorSummary),
    supportContext: {
      readOnly: true,
      mutationEndpointsEnabled: false,
      repairEndpointsEnabled: false,
      source: "admin_player_detail"
    }
  });
});
app.post("/v1/admin/players/:accountId/notes", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (principal.role === "VIEWER") {
    return c.json({ error: "ADMIN_PERMISSION_DENIED" }, 403);
  }
  const accountId = c.req.param("accountId");
  const body = await c.req.json();
  const note = typeof body.note === "string" ? body.note.trim() : "";
  if (accountId.length === 0 || note.length === 0 || note.length > 1e3) {
    return c.json({ error: "PLAYER_NOTE_INVALID" }, 400);
  }
  const account = await c.env.DB.prepare("SELECT id FROM accounts WHERE id = ?").bind(accountId).first();
  if (account === null) {
    return c.json({ error: "ADMIN_PLAYER_NOT_FOUND" }, 404);
  }
  const requestId = c.req.header("x-request-id") ?? createRequestId();
  const changeSummary = { note };
  const payloadHash = await createPayloadHash({ accountId, ...changeSummary });
  await c.env.DB.prepare(
    `INSERT INTO admin_audit_logs (
       id,
       principal_id,
       role,
       permission,
       action,
       resource_type,
       resource_id,
       request_id,
       confirmation_id,
       payload_hash,
       change_summary_json,
       account_mutation_id,
       result,
       error_code,
       created_at
     ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, NULL, ?, ?, NULL, ?, NULL, ?)`
  ).bind(
    `audit_${createRequestId().slice(4)}`,
    principal.id,
    principal.role,
    ACCOUNT_NOTE_WRITE_PERMISSION,
    "PLAYER_NOTE_CREATE",
    "account",
    account.id,
    requestId,
    payloadHash,
    canonicalJson(changeSummary),
    "SUCCESS",
    Date.now()
  ).run();
  return c.json(
    {
      note: {
        accountId: account.id,
        action: "PLAYER_NOTE_CREATE",
        result: "SUCCESS",
        requestId
      }
    },
    201
  );
});
app.get("/v1/admin/players/:accountId/notes", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  const accountId = c.req.param("accountId");
  const rows = await c.env.DB.prepare(
    `SELECT
       audit.id AS id,
       audit.principal_id AS principal_id,
       principal.display_name AS display_name,
       audit.resource_id AS resource_id,
       audit.request_id AS request_id,
       audit.change_summary_json AS change_summary_json,
       audit.created_at AS created_at
     FROM admin_audit_logs audit
     LEFT JOIN admin_principals principal ON principal.id = audit.principal_id
     WHERE audit.action = 'PLAYER_NOTE_CREATE'
       AND audit.resource_type = 'account'
       AND audit.resource_id = ?
       AND audit.result = 'SUCCESS'
     ORDER BY audit.created_at DESC
     LIMIT 20`
  ).bind(accountId).all();
  return c.json({
    items: rows.results.map((row) => ({
      id: row.id,
      accountId: row.resource_id,
      note: parsePlayerNoteSummary(row.change_summary_json),
      authorId: row.principal_id,
      authorDisplayName: row.display_name ?? row.principal_id,
      requestId: row.request_id,
      createdAt: toIso(row.created_at)
    }))
  });
});
app.get("/v1/admin/audit", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  const action = c.req.query("action")?.trim() || null;
  const resourceId = c.req.query("resourceId")?.trim() || null;
  const result = c.req.query("result")?.trim() || null;
  if (result !== null && result !== "SUCCESS" && result !== "FAILURE") {
    return c.json({ error: "AUDIT_FILTER_INVALID" }, 400);
  }
  const rows = await c.env.DB.prepare(
    `SELECT
       audit.id AS id,
       audit.principal_id AS principal_id,
       principal.display_name AS display_name,
       audit.role AS role,
       audit.permission AS permission,
       audit.action AS action,
       audit.resource_type AS resource_type,
       audit.resource_id AS resource_id,
       audit.request_id AS request_id,
       audit.confirmation_id AS confirmation_id,
       audit.payload_hash AS payload_hash,
       audit.change_summary_json AS change_summary_json,
       audit.result AS result,
       audit.error_code AS error_code,
       audit.created_at AS created_at
     FROM admin_audit_logs audit
     LEFT JOIN admin_principals principal ON principal.id = audit.principal_id
     WHERE (? IS NULL OR audit.action = ?)
       AND (? IS NULL OR audit.resource_id = ?)
       AND (? IS NULL OR audit.result = ?)
     ORDER BY audit.created_at DESC
     LIMIT 100`
  ).bind(action, action, resourceId, resourceId, result, result).all();
  return c.json({
    items: rows.results.map((row) => ({
      id: row.id,
      principalId: row.principal_id,
      principalDisplayName: row.display_name ?? row.principal_id ?? "Unknown",
      role: row.role,
      permission: row.permission,
      action: row.action,
      resourceType: row.resource_type,
      resourceId: row.resource_id,
      requestId: row.request_id,
      confirmationId: row.confirmation_id,
      payloadHash: row.payload_hash,
      changeSummary: parseJsonValue(row.change_summary_json),
      result: row.result,
      errorCode: row.error_code,
      createdAt: toIso(row.created_at)
    }))
  });
});
app.get("/v1/admin/integrity/events", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  const accountId = c.req.query("accountId")?.trim() || null;
  const classification = c.req.query("classification")?.trim() || null;
  const reviewState = c.req.query("reviewState")?.trim() || null;
  if (classification !== null && classification !== "SOFT_ANOMALY" && classification !== "HARD_VIOLATION") {
    return c.json({ error: "INTEGRITY_FILTER_INVALID" }, 400);
  }
  if (reviewState !== null && reviewState !== "OPEN" && reviewState !== "IN_REVIEW" && reviewState !== "RESOLVED") {
    return c.json({ error: "INTEGRITY_FILTER_INVALID" }, 400);
  }
  const rows = await c.env.DB.prepare(
    `SELECT
       event.id AS id,
       event.account_id AS account_id,
       event.run_id AS run_id,
       event.classification AS classification,
       event.category AS category,
       event.evidence_code AS evidence_code,
       event.evidence_json AS evidence_json,
       COALESCE(review.state, 'OPEN') AS review_state,
       review.assigned_to AS assigned_to,
       event.created_at AS created_at
     FROM integrity_events event
     LEFT JOIN integrity_event_review_states review ON review.event_id = event.id
     WHERE (? IS NULL OR event.account_id = ?)
       AND (? IS NULL OR event.classification = ?)
       AND (? IS NULL OR COALESCE(review.state, 'OPEN') = ?)
     ORDER BY event.created_at DESC
     LIMIT 100`
  ).bind(accountId, accountId, classification, classification, reviewState, reviewState).all();
  return c.json({
    items: rows.results.map((row) => ({
      id: row.id,
      accountId: row.account_id,
      runId: row.run_id,
      classification: row.classification,
      category: row.category,
      evidenceCode: row.evidence_code,
      evidence: parseJsonValue(row.evidence_json),
      reviewState: row.review_state,
      assignedTo: row.assigned_to,
      createdAt: toIso(row.created_at)
    }))
  });
});
app.get("/v1/admin/integrity/suspicious-players", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  const reviewStatus = c.req.query("reviewStatus")?.trim() || null;
  if (reviewStatus !== null && reviewStatus !== "NEW" && reviewStatus !== "REVIEWING" && reviewStatus !== "CLEARED" && reviewStatus !== "SCORE_EXCLUDED" && reviewStatus !== "REWARD_HELD" && reviewStatus !== "ESCALATED" && reviewStatus !== "ACTIONED" && reviewStatus !== "FALSE_POSITIVE") {
    return c.json({ error: "SUSPICIOUS_PLAYERS_FILTER_INVALID" }, 400);
  }
  const rows = await c.env.DB.prepare(
    `SELECT
       event.account_id AS account_id,
       profile.display_name AS display_name,
       COUNT(*) AS open_signals,
       SUM(CASE WHEN event.classification = 'HARD_VIOLATION' THEN 1 ELSE 0 END) AS hard_violations,
       COUNT(DISTINCT event.run_id) AS suspicious_runs,
       MAX(event.created_at) AS last_detection_at,
       latest.evidence_code AS primary_reason,
       COALESCE(
         MAX(account_review.status),
         CASE MAX(CASE WHEN COALESCE(review.state, 'OPEN') = 'IN_REVIEW' THEN 1 ELSE 0 END)
           WHEN 1 THEN 'REVIEWING'
           ELSE 'NEW'
         END
       ) AS review_status,
       enforcement.type AS current_enforcement,
       COALESCE(MAX(account_review.assigned_to), MAX(review.assigned_to)) AS review_owner
     FROM integrity_events event
     LEFT JOIN player_profiles profile ON profile.account_id = event.account_id
     LEFT JOIN integrity_event_review_states review ON review.event_id = event.id
     LEFT JOIN suspicious_player_reviews account_review ON account_review.account_id = event.account_id
     LEFT JOIN account_enforcements enforcement
       ON enforcement.account_id = event.account_id
      AND enforcement.status = 'ACTIVE'
     LEFT JOIN integrity_events latest
       ON latest.id = (
         SELECT inner_event.id
         FROM integrity_events inner_event
         WHERE inner_event.account_id = event.account_id
         ORDER BY inner_event.created_at DESC
         LIMIT 1
       )
     WHERE COALESCE(review.state, 'OPEN') IN ('OPEN', 'IN_REVIEW')
     GROUP BY event.account_id
     HAVING (? IS NULL OR
       COALESCE(
         MAX(account_review.status),
         CASE MAX(CASE WHEN COALESCE(review.state, 'OPEN') = 'IN_REVIEW' THEN 1 ELSE 0 END)
           WHEN 1 THEN 'REVIEWING'
           ELSE 'NEW'
         END
       ) = ?)
     ORDER BY last_detection_at DESC
     LIMIT 100`
  ).bind(reviewStatus, reviewStatus).all();
  return c.json({
    items: rows.results.map((row) => {
      const review = suspiciousReviewStatus(row.review_status);
      const markOnlyStatus = currentMarkOnlyStatus(review);
      return {
        accountId: row.account_id,
        displayName: row.display_name ?? row.account_id,
        priority: suspiciousPriority(row.open_signals, row.hard_violations, [row.primary_reason]),
        openSignals: row.open_signals,
        hardViolations: row.hard_violations,
        suspiciousRuns: row.suspicious_runs,
        lastDetectionAt: toIso(row.last_detection_at),
        primaryReason: row.primary_reason,
        reviewStatus: review,
        currentEnforcement: markOnlyStatus === "NONE" ? currentEnforcementStatus(row.current_enforcement) : markOnlyStatus,
        reviewOwner: row.review_owner
      };
    })
  });
});
app.get("/v1/admin/integrity/suspicious-players/:accountId", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  const accountId = c.req.param("accountId");
  const account = await c.env.DB.prepare(
    `SELECT account.id AS account_id,
            profile.display_name AS display_name,
            account.status AS status,
            account.created_at AS created_at
     FROM accounts account
     LEFT JOIN player_profiles profile ON profile.account_id = account.id
     WHERE account.id = ?`
  ).bind(accountId).first();
  if (account === null) {
    return c.json({ error: "ACCOUNT_NOT_FOUND" }, 404);
  }
  const review = await c.env.DB.prepare(
    `SELECT status,
            assigned_to,
            reason,
            updated_at
     FROM suspicious_player_reviews
     WHERE account_id = ?`
  ).bind(accountId).first();
  const signals = await c.env.DB.prepare(
    `SELECT event.id AS id,
            event.account_id AS account_id,
            event.run_id AS run_id,
            event.classification AS classification,
            event.category AS category,
            event.evidence_code AS evidence_code,
            event.evidence_json AS evidence_json,
            COALESCE(review.state, 'OPEN') AS review_state,
            review.assigned_to AS assigned_to,
            event.created_at AS created_at
     FROM integrity_events event
     LEFT JOIN integrity_event_review_states review ON review.event_id = event.id
     WHERE event.account_id = ?
     ORDER BY event.created_at DESC
     LIMIT 100`
  ).bind(accountId).all();
  const runIds = [...new Set(signals.results.map((signal) => signal.run_id).filter((runId) => runId !== null))];
  const runs = runIds.length > 0 ? await c.env.DB.prepare(
    `SELECT run.id AS run_id,
                run.status AS status,
                preview.risk AS risk,
                run.balance_config_version AS balance_config_version,
                run.started_at AS started_at,
                run.finished_at AS finished_at
         FROM runs run
         LEFT JOIN operation_previews preview ON preview.id = run.preview_id
         WHERE run.id IN (${runIds.map(() => "?").join(", ")})
         ORDER BY run.started_at DESC`
  ).bind(...runIds).all() : { results: [] };
  const notes = await c.env.DB.prepare(
    `SELECT audit.id AS id,
            audit.principal_id AS principal_id,
            principal.display_name AS display_name,
            audit.resource_id AS resource_id,
            audit.request_id AS request_id,
            audit.change_summary_json AS change_summary_json,
            audit.created_at AS created_at
     FROM admin_audit_logs audit
     LEFT JOIN admin_principals principal ON principal.id = audit.principal_id
     WHERE audit.action = 'PLAYER_NOTE_CREATE'
       AND audit.resource_id = ?
     ORDER BY audit.created_at DESC
     LIMIT 25`
  ).bind(accountId).all();
  const decisions = await c.env.DB.prepare(
    `SELECT history.id AS id,
            principal.display_name AS display_name,
            history.from_status AS from_status,
            history.to_status AS to_status,
            history.reason AS reason,
            history.created_at AS created_at
     FROM suspicious_player_review_history history
     LEFT JOIN admin_principals principal ON principal.id = history.created_by
     WHERE history.account_id = ?
     ORDER BY history.created_at DESC
     LIMIT 25`
  ).bind(accountId).all();
  const hardViolations = signals.results.filter((signal) => signal.classification === "HARD_VIOLATION").length;
  const derivedReviewOwner = signals.results.find((signal) => signal.assigned_to !== null)?.assigned_to ?? null;
  const hasReviewingSignal = signals.results.some((signal) => suspiciousReviewStatus(signal.review_state) === "REVIEWING");
  const evidenceCodes = signals.results.map((signal) => signal.evidence_code);
  const accountReviewStatus = review ? suspiciousReviewStatus(review.status) : null;
  const derivedReviewStatus = hasReviewingSignal ? "REVIEWING" : "NEW";
  return c.json({
    account: {
      accountId: account.account_id,
      displayName: account.display_name ?? account.account_id,
      status: account.status,
      createdAt: toIso(account.created_at)
    },
    summary: {
      priority: suspiciousPriority(signals.results.length, hardViolations, evidenceCodes),
      openSignals: signals.results.length,
      hardViolations,
      suspiciousRuns: runIds.length,
      lastDetectionAt: signals.results[0] ? toIso(signals.results[0].created_at) : null,
      primaryReason: signals.results[0]?.evidence_code ?? null,
      reviewStatus: accountReviewStatus ?? derivedReviewStatus,
      reviewReason: review?.reason ?? null,
      reviewUpdatedAt: review ? toIso(review.updated_at) : null,
      currentEnforcement: currentMarkOnlyStatus(accountReviewStatus),
      reviewOwner: review?.assigned_to ?? derivedReviewOwner
    },
    riskSignals: signals.results.map((signal) => ({
      id: signal.id,
      runId: signal.run_id,
      classification: signal.classification,
      category: signal.category,
      evidenceCode: signal.evidence_code,
      evidence: parseJsonValue(signal.evidence_json),
      reviewStatus: suspiciousReviewStatus(signal.review_state),
      reviewOwner: signal.assigned_to,
      createdAt: toIso(signal.created_at)
    })),
    suspiciousRuns: runs.results.map((run) => ({
      runId: run.run_id,
      runType: suspiciousRunType(run.run_id),
      result: mapRunResult(run.status),
      riskTier: normalizeRiskTier(run.risk),
      balanceVersion: String(run.balance_config_version),
      startedAt: toIso(run.started_at),
      completedAt: run.finished_at === null ? null : toIso(run.finished_at)
    })),
    notes: notes.results.map((row) => ({
      id: row.id,
      accountId: row.resource_id,
      note: parsePlayerNoteSummary(row.change_summary_json),
      authorId: row.principal_id,
      authorDisplayName: row.display_name ?? row.principal_id,
      requestId: row.request_id,
      createdAt: toIso(row.created_at)
    })),
    decisions: decisions.results.map((row) => ({
      id: row.id,
      action: "SUSPICIOUS_PLAYER_REVIEW_MARK",
      result: "SUCCESS",
      principalDisplayName: row.display_name ?? "Unknown Admin",
      changeSummary: {
        fromStatus: row.from_status,
        toStatus: row.to_status,
        reason: row.reason
      },
      createdAt: toIso(row.created_at)
    }))
  });
});
app.get("/v1/admin/runs", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  const accountId = c.req.query("accountId")?.trim();
  const rows = await c.env.DB.prepare(
    `SELECT
       r.id AS run_id,
       r.account_id AS account_id,
       r.status AS status,
       r.started_at AS started_at,
       r.finished_at AS finished_at,
       op.risk AS risk,
       r.balance_config_version AS balance_config_version,
       vv.version AS validator_version,
       rs.snapshot_json AS snapshot_json,
       final.payload_json AS final_payload_json
     FROM runs r
     LEFT JOIN run_snapshots rs ON rs.run_id = r.id
     LEFT JOIN run_final_results final ON final.run_id = r.id
     LEFT JOIN operation_previews op ON op.id = r.preview_id
     LEFT JOIN validator_runtime_active vra ON vra.singleton_id = 1
     LEFT JOIN validator_runtime_configs vrc ON vrc.id = vra.runtime_config_id
     LEFT JOIN validator_versions vv ON vv.id = vrc.validator_version_id
     WHERE (? IS NULL OR r.account_id = ?)
     ORDER BY r.started_at DESC
     LIMIT 50`
  ).bind(accountId ?? null, accountId ?? null).all();
  return c.json({
    items: rows.results.map((row) => ({
      runId: row.run_id,
      accountId: row.account_id,
      startedAt: toIso(row.started_at),
      ...row.finished_at ? { completedAt: toIso(row.finished_at) } : {},
      result: mapRunResult(row.status),
      riskTier: normalizeRiskTier(row.risk),
      balanceVersion: String(row.balance_config_version),
      ...row.validator_version ? { validatorVersion: row.validator_version } : {},
      runStartVersionSnapshot: parseAdminRunStartVersionSnapshot(row.snapshot_json),
      runStartVersionSnapshotStatus: adminRunStartVersionSnapshotStatus(row.snapshot_json),
      runFinishVersionSnapshot: parseAdminRunFinishVersionSnapshot(row.final_payload_json),
      runFinishVersionSnapshotStatus: adminRunFinishVersionSnapshotStatus(row)
    }))
  });
});
app.get("/v1/admin/runs/:runId", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (hasRequestBody(c)) return rejectAdminRunBody(c);
  const row = await c.env.DB.prepare(
    `SELECT
       r.id AS run_id,
       r.account_id AS account_id,
       r.status AS status,
       r.started_at AS started_at,
       r.finished_at AS finished_at,
       op.risk AS risk,
       r.balance_config_version AS balance_config_version,
       active.version AS current_active_balance_version,
       r.build_id AS build_id,
       cb.content_version AS content_version,
       vv.version AS validator_version,
       vrc.runtime_revision AS validator_runtime_revision,
       rs.snapshot_json AS snapshot_json,
       op.preview_json AS preview_json
     FROM runs r
     INNER JOIN run_snapshots rs ON rs.run_id = r.id
     LEFT JOIN operation_previews op ON op.id = r.preview_id
     LEFT JOIN client_builds cb ON cb.id = r.build_id
     LEFT JOIN balance_config_active active ON active.singleton_id = 1
     LEFT JOIN validator_runtime_active vra ON vra.singleton_id = 1
     LEFT JOIN validator_runtime_configs vrc ON vrc.id = vra.runtime_config_id
     LEFT JOIN validator_versions vv ON vv.id = vrc.validator_version_id
     WHERE r.id = ?`
  ).bind(c.req.param("runId")).first();
  if (row === null) {
    return c.json({ error: "ADMIN_RUN_NOT_FOUND" }, 404);
  }
  const finalResult = await c.env.DB.prepare(
    `SELECT outcome, cleared_waves, payload_json, submitted_at
     FROM run_final_results
     WHERE run_id = ?`
  ).bind(row.run_id).first();
  const rewardPreview = await c.env.DB.prepare(
    `SELECT id AS reward_preview_id,
            status AS preview_status,
            reward_claim_id AS reward_claim_id,
            reward_plan_json AS reward_plan_json
     FROM run_reward_previews
     WHERE run_id = ?`
  ).bind(row.run_id).first();
  const previewMetadata = parseRunPreviewMetadata(row.preview_json ?? null);
  const firstClear = previewMetadata.operationId === null ? null : await c.env.DB.prepare(
    `SELECT operation_key, risk, cleared_at, run_id
         FROM account_first_clears
         WHERE account_id = ? AND operation_key = ? AND risk = ?`
  ).bind(row.account_id, previewMetadata.operationId, normalizeRiskTier(row.risk)).first();
  const finalMetadata = parseRunFinalPayloadMetadata(finalResult?.payload_json);
  const fuelLedgerRows = finalMetadata.staminaRefund === null ? { results: [] } : await c.env.DB.prepare(
    `SELECT id, delta, reason
         FROM fuel_ledger
         WHERE account_id = ? AND reason = 'STORY_RAID_FAILURE_REFUND'
         ORDER BY created_at DESC, id
         LIMIT 5`
  ).bind(row.account_id).all();
  const integrity = await c.env.DB.prepare(
    `SELECT
       COUNT(event.id) AS open_signals,
       COALESCE(SUM(CASE WHEN event.classification = 'HARD_VIOLATION' THEN 1 ELSE 0 END), 0) AS hard_violations,
       (
         SELECT latest.evidence_code
         FROM integrity_events latest
         WHERE latest.run_id = ?
         ORDER BY latest.created_at DESC, latest.id DESC
         LIMIT 1
       ) AS primary_reason
     FROM integrity_events event
     LEFT JOIN integrity_event_review_states review ON review.event_id = event.id
     WHERE event.run_id = ?
       AND COALESCE(review.state, 'OPEN') != 'RESOLVED'`
  ).bind(row.run_id, row.run_id).first();
  const rewardCalculation = parseRunRewardCalculationMetadata(rewardPreview?.reward_plan_json);
  const runStartVersionSnapshot = parseAdminRunStartVersionSnapshot(row.snapshot_json);
  const runFinishVersionSnapshot = parseAdminRunFinishVersionSnapshot(finalResult?.payload_json);
  const fuelLedgerRefs = validFuelLedgerRows(fuelLedgerRows.results).map((ledger) => ledger.id);
  const staminaRefund = finalMetadata.staminaRefund === null ? null : {
    ...finalMetadata.staminaRefund,
    fuelLedgerRefs
  };
  const openSignals = integrity?.open_signals ?? 0;
  const hardViolations = integrity?.hard_violations ?? 0;
  return c.json({
    runId: row.run_id,
    accountId: row.account_id,
    startedAt: toIso(row.started_at),
    ...row.finished_at ? { completedAt: toIso(row.finished_at) } : {},
    result: mapRunResult(row.status),
    riskTier: normalizeRiskTier(row.risk),
    balanceVersion: String(row.balance_config_version),
    currentActiveBalanceVersion: String(row.current_active_balance_version ?? row.balance_config_version),
    clientBuild: row.build_id ?? "UNKNOWN",
    contentVersion: String(row.content_version ?? 0),
    validatorVersion: row.validator_version ?? "UNKNOWN",
    validatorRuntimeRevision: String(row.validator_runtime_revision ?? 0),
    snapshot: parseRunSnapshot(row.snapshot_json),
    runVersionSnapshot: runStartVersionSnapshot,
    runStartVersionSnapshot,
    runStartVersionSnapshotStatus: adminRunStartVersionSnapshotStatus(row.snapshot_json),
    runFinishVersionSnapshot,
    runFinishVersionSnapshotStatus: adminRunFinishVersionSnapshotStatus({
      finished_at: row.finished_at,
      final_payload_json: finalResult?.payload_json ?? null
    }),
    operationId: previewMetadata.operationId,
    outcome: finalResult?.outcome ?? null,
    clearedWaves: finalResult?.cleared_waves ?? null,
    rewardPreviewId: rewardPreview?.reward_preview_id ?? null,
    rewardClaimId: rewardPreview?.reward_claim_id ?? null,
    appliedMultiplierLabel: rewardCalculation.appliedMultiplierLabel,
    calculationMetadataStatus: rewardCalculation.calculationMetadataStatus,
    firstClearConsumed: firstClear !== null && firstClear !== void 0,
    integrityDisposition: {
      status: hardViolations > 0 ? "HIGH_RISK" : openSignals > 0 ? "NEEDS_REVIEW" : "CLEAR",
      openSignals,
      hardViolations,
      primaryReason: integrity?.primary_reason ?? null
    },
    encounterType: previewMetadata.encounterType,
    raidId: finalMetadata.raidId ?? previewMetadata.raidId,
    bossId: finalMetadata.bossId ?? previewMetadata.bossId,
    bossPhaseRefs: previewMetadata.bossPhaseRefs,
    bossDefeated: finalMetadata.bossDefeated,
    completedPhaseCount: finalMetadata.completedPhaseCount,
    encounterDurationSeconds: finalMetadata.encounterDurationSeconds,
    allowedOperatorSlots: previewMetadata.allowedOperatorSlots,
    startAugmentPackageRef: previewMetadata.startAugmentPackageRef,
    startTacticPackageRef: previewMetadata.startTacticPackageRef,
    selectedAugmentIds: finalMetadata.selectedAugmentIds,
    selectedTacticIds: finalMetadata.selectedTacticIds,
    staminaRefund,
    supportContext: {
      readOnly: true,
      mutationEndpointsEnabled: false,
      repairEndpointsEnabled: false,
      source: "admin_run_detail"
    }
  });
});
app.post("/v1/admin/balance-config/drafts", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (principal.role === "VIEWER") {
    return c.json({ error: "ADMIN_PERMISSION_DENIED" }, 403);
  }
  const body = await c.req.json();
  if (!(body.baseVersion === void 0 || body.baseVersion === null || isPositiveInteger(body.baseVersion)) || !isPositiveInteger(body.schemaVersion) || !isNonNegativeInteger(body.requiredContentVersion) || !isJsonObject(body.config)) {
    return c.json({ error: "BALANCE_DRAFT_INVALID" }, 400);
  }
  const now = Date.now();
  const draftId = `draft_${createRequestId().slice(4)}`;
  const configJson = canonicalJson(body.config);
  const checksum = `sha256:${await sha256Hex(configJson)}`;
  const baseVersion = body.baseVersion ?? null;
  await c.env.DB.prepare(
    `INSERT INTO balance_config_drafts (
       id,
       base_version,
       schema_version,
       required_content_version,
       config_json,
       checksum,
       revision,
       status,
       created_by,
       created_at,
       updated_at
     ) VALUES (?, ?, ?, ?, ?, ?, 1, 'DRAFT', ?, ?, ?)`
  ).bind(draftId, baseVersion, body.schemaVersion, body.requiredContentVersion, configJson, checksum, principal.id, now, now).run();
  return c.json(
    {
      draft: {
        id: draftId,
        baseVersion,
        schemaVersion: body.schemaVersion,
        requiredContentVersion: body.requiredContentVersion,
        checksum,
        revision: 1,
        status: "DRAFT"
      }
    },
    201
  );
});
app.post("/v1/admin/balance-config/drafts/:draftId/publish/confirmations", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (principal.role !== "OWNER") {
    await writeBalancePublishFailureAudit(c, principal, c.req.param("draftId"), "ADMIN_PERMISSION_DENIED", null, { reason: "role_not_owner" });
    return c.json({ error: "ADMIN_PERMISSION_DENIED" }, 403);
  }
  const draftId = c.req.param("draftId");
  const body = await c.req.json();
  if (draftId.length === 0 || !isPositiveInteger(body.expectedRevision) || typeof body.expectedChecksum !== "string" || body.expectedChecksum.length === 0) {
    return c.json({ error: "BALANCE_PUBLISH_CONFIRMATION_INVALID" }, 400);
  }
  const draft = await c.env.DB.prepare(
    `SELECT id, base_version, schema_version, required_content_version, config_json, checksum, revision, status
     FROM balance_config_drafts
     WHERE id = ?
       AND status = 'DRAFT'`
  ).bind(draftId).first();
  if (draft === null) {
    return c.json({ error: "BALANCE_DRAFT_NOT_FOUND" }, 404);
  }
  if (draft.revision !== body.expectedRevision || draft.checksum !== body.expectedChecksum) {
    await writeBalancePublishFailureAudit(c, principal, draft.id, "BALANCE_DRAFT_PRECONDITION_FAILED", null, {
      expectedRevision: body.expectedRevision,
      actualRevision: draft.revision,
      expectedChecksum: body.expectedChecksum,
      actualChecksum: draft.checksum
    });
    return c.json({ error: "BALANCE_DRAFT_PRECONDITION_FAILED" }, 409);
  }
  const active = await c.env.DB.prepare(
    `SELECT version
     FROM balance_config_active
     WHERE singleton_id = 1`
  ).bind().first();
  const activeVersion = active?.version ?? null;
  if (draft.base_version !== activeVersion) {
    await writeBalancePublishFailureAudit(c, principal, draft.id, "BALANCE_DRAFT_STALE_BASE", null, {
      draftBaseVersion: draft.base_version,
      activeVersion
    });
    return c.json({ error: "BALANCE_DRAFT_STALE_BASE" }, 409);
  }
  const now = Date.now();
  const expiresAt = now + CONFIRMATION_TTL_MS;
  const confirmationId = `confirmation_${createRequestId().slice(4)}`;
  const token = `confirm_${createRequestId().slice(4)}`;
  const payloadHash = await createBalancePublishPayloadHash(draft.id, body.expectedRevision, body.expectedChecksum);
  const tokenHash = await createTokenHash(token);
  await c.env.DB.batch([
    c.env.DB.prepare(
      `INSERT INTO admin_confirmations (
         id,
         principal_id,
         permission,
         payload_hash,
         token_hash,
         expires_at,
         consumed_at,
         created_at
       ) VALUES (?, ?, ?, ?, ?, ?, NULL, ?)`
    ).bind(confirmationId, principal.id, BALANCE_PUBLISH_PERMISSION, payloadHash, tokenHash, expiresAt, now)
  ]);
  return c.json(
    {
      confirmation: {
        id: confirmationId,
        permission: BALANCE_PUBLISH_PERMISSION,
        payloadHash,
        expiresAt,
        token
      }
    },
    201
  );
});
app.post("/v1/admin/balance-config/drafts/:draftId/publish", async (c) => {
  const principal = await authenticateAdmin(c);
  if (principal instanceof Response) return principal;
  if (principal.role !== "OWNER") {
    await writeBalancePublishFailureAudit(c, principal, c.req.param("draftId"), "ADMIN_PERMISSION_DENIED", null, { reason: "role_not_owner" });
    return c.json({ error: "ADMIN_PERMISSION_DENIED" }, 403);
  }
  const draftId = c.req.param("draftId");
  const body = await c.req.json();
  if (draftId.length === 0 || !isPositiveInteger(body.expectedRevision) || typeof body.expectedChecksum !== "string" || body.expectedChecksum.length === 0 || typeof body.confirmationToken !== "string" || body.confirmationToken.length === 0) {
    return c.json({ error: "BALANCE_PUBLISH_INVALID" }, 400);
  }
  const payloadHash = await createBalancePublishPayloadHash(draftId, body.expectedRevision, body.expectedChecksum);
  const draft = await c.env.DB.prepare(
    `SELECT id, base_version, schema_version, required_content_version, config_json, checksum, revision, status
     FROM balance_config_drafts
     WHERE id = ?
       AND status = 'DRAFT'`
  ).bind(draftId).first();
  if (draft === null) {
    await writeBalancePublishFailureAudit(c, principal, draftId, "BALANCE_DRAFT_NOT_FOUND", payloadHash, { draftId });
    return c.json({ error: "BALANCE_DRAFT_NOT_FOUND" }, 404);
  }
  if (draft.revision !== body.expectedRevision || draft.checksum !== body.expectedChecksum) {
    await writeBalancePublishFailureAudit(c, principal, draft.id, "BALANCE_DRAFT_PRECONDITION_FAILED", payloadHash, {
      expectedRevision: body.expectedRevision,
      actualRevision: draft.revision,
      expectedChecksum: body.expectedChecksum,
      actualChecksum: draft.checksum
    });
    return c.json({ error: "BALANCE_DRAFT_PRECONDITION_FAILED" }, 409);
  }
  const active = await c.env.DB.prepare(
    `SELECT version
     FROM balance_config_active
     WHERE singleton_id = 1`
  ).bind().first();
  const activeVersion = active?.version ?? null;
  if (draft.base_version !== activeVersion) {
    await writeBalancePublishFailureAudit(c, principal, draft.id, "BALANCE_DRAFT_STALE_BASE", payloadHash, {
      draftBaseVersion: draft.base_version,
      activeVersion
    });
    return c.json({ error: "BALANCE_DRAFT_STALE_BASE" }, 409);
  }
  const nextVersionRow = await c.env.DB.prepare(
    `SELECT COALESCE(MAX(version), 0) + 1 AS next_version
     FROM balance_config_versions`
  ).bind().first();
  const nextVersion = nextVersionRow?.next_version ?? 1;
  const now = Date.now();
  const tokenHash = await createTokenHash(body.confirmationToken);
  const confirmation = await c.env.DB.prepare(
    `SELECT id, payload_hash
     FROM admin_confirmations
     WHERE principal_id = ?
       AND permission = ?
       AND payload_hash = ?
       AND token_hash = ?
       AND consumed_at IS NULL
       AND expires_at > ?`
  ).bind(principal.id, BALANCE_PUBLISH_PERMISSION, payloadHash, tokenHash, now).first();
  if (confirmation === null) {
    await writeBalancePublishFailureAudit(c, principal, draft.id, "ADMIN_CONFIRMATION_INVALID", payloadHash, {
      expectedRevision: body.expectedRevision,
      expectedChecksum: body.expectedChecksum
    });
    return c.json({ error: "ADMIN_CONFIRMATION_INVALID" }, 409);
  }
  const publishClaimId = `publish_${createRequestId().slice(4)}`;
  const activationId = `activation_${createRequestId().slice(4)}`;
  const requestId = createRequestId();
  const batchResults = await c.env.DB.batch([
    c.env.DB.prepare(
      `UPDATE admin_confirmations
       SET consumed_at = ?
       WHERE id = ?
         AND consumed_at IS NULL
         AND expires_at > ?`
    ).bind(now, confirmation.id, now),
    c.env.DB.prepare(
      `INSERT INTO balance_config_versions (
         version,
         schema_version,
         required_content_version,
         checksum,
         config_json,
         published_at,
         published_by,
         draft_id
       )
       SELECT ?, ?, ?, ?, ?, ?, ?, ?
       WHERE EXISTS (
         SELECT 1
         FROM admin_confirmations
         WHERE id = ?
           AND consumed_at = ?
       )`
    ).bind(nextVersion, draft.schema_version, draft.required_content_version, draft.checksum, draft.config_json, now, principal.id, draft.id, confirmation.id, now),
    c.env.DB.prepare(
      `INSERT INTO balance_config_publish_claims (
         id,
         draft_id,
         expected_revision,
         expected_checksum,
         published_version,
         created_at
       )
       SELECT ?, ?, ?, ?, ?, ?
       WHERE EXISTS (
         SELECT 1
         FROM admin_confirmations
         WHERE id = ?
           AND consumed_at = ?
       )`
    ).bind(publishClaimId, draft.id, body.expectedRevision, body.expectedChecksum, nextVersion, now, confirmation.id, now),
    c.env.DB.prepare(
      `UPDATE balance_config_drafts
       SET status = 'PUBLISHED',
           updated_at = ?
       WHERE id = ?
         AND status = 'DRAFT'
         AND EXISTS (
           SELECT 1
           FROM admin_confirmations
           WHERE id = ?
             AND consumed_at = ?
         )`
    ).bind(now, draft.id, confirmation.id, now),
    c.env.DB.prepare(
      `INSERT INTO balance_config_activations (
         id,
         version,
         activation_type,
         activated_by,
         activated_at,
         reason
       )
       SELECT ?, ?, 'PUBLISH', ?, ?, ?
       WHERE EXISTS (
         SELECT 1
         FROM admin_confirmations
         WHERE id = ?
           AND consumed_at = ?
       )`
    ).bind(activationId, nextVersion, principal.id, now, null, confirmation.id, now),
    c.env.DB.prepare(
      `INSERT INTO balance_config_active (
         singleton_id,
         version,
         activation_id,
         updated_at
       )
       SELECT 1, ?, ?, ?
       WHERE EXISTS (
         SELECT 1
         FROM admin_confirmations
         WHERE id = ?
           AND consumed_at = ?
       )
       ON CONFLICT(singleton_id) DO UPDATE SET
         version = excluded.version,
         activation_id = excluded.activation_id,
         updated_at = excluded.updated_at`
    ).bind(nextVersion, activationId, now, confirmation.id, now),
    c.env.DB.prepare(
      `INSERT INTO admin_audit_logs (
         id,
         principal_id,
         role,
         permission,
         action,
         resource_type,
         resource_id,
         request_id,
         confirmation_id,
         payload_hash,
         change_summary_json,
         account_mutation_id,
         result,
         error_code,
         created_at
       )
       SELECT ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, NULL, 'SUCCESS', NULL, ?
       WHERE EXISTS (
         SELECT 1
         FROM admin_confirmations
         WHERE id = ?
           AND consumed_at = ?
       )`
    ).bind(
      `audit_${createRequestId().slice(4)}`,
      principal.id,
      principal.role,
      BALANCE_PUBLISH_PERMISSION,
      "BALANCE_CONFIG_PUBLISH",
      "balance_config_draft",
      draft.id,
      requestId,
      confirmation.id,
      confirmation.payload_hash,
      canonicalJson({ publishedVersion: nextVersion, activationId, checksum: draft.checksum }),
      now,
      confirmation.id,
      now
    )
  ]);
  if (d1Changes(batchResults[0]) !== 1) {
    await writeBalancePublishFailureAudit(c, principal, draft.id, "ADMIN_CONFIRMATION_CONSUME_FAILED", payloadHash, {
      confirmationId: confirmation.id,
      expectedRevision: body.expectedRevision,
      expectedChecksum: body.expectedChecksum
    });
    return c.json({ error: "ADMIN_CONFIRMATION_CONSUME_FAILED" }, 409);
  }
  return c.json(
    {
      publish: {
        draftId: draft.id,
        version: nextVersion,
        activationId,
        checksum: draft.checksum,
        confirmationId: confirmation.id,
        requestId
      }
    },
    201
  );
});
var index_default = app;
export {
  index_default as default
};
//# sourceMappingURL=index.js.map
