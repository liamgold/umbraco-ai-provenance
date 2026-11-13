import { LitElement as x, html as y, css as W, state as l, customElement as w } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin as C } from "@umbraco-cms/backoffice/element-api";
import { UMB_NOTIFICATION_CONTEXT as T } from "@umbraco-cms/backoffice/notification";
import { UMB_CURRENT_USER_CONTEXT as E } from "@umbraco-cms/backoffice/current-user";
import { tryExecute as p } from "@umbraco-cms/backoffice/resources";
import { c as _ } from "./client.gen-C4MoLlwI.js";
const M = (e) => (e?.client ?? _).get({
  security: [{ scheme: "bearer", type: "http" }],
  url: "/umbraco/umbracocommunityaiprovenance/api/v1/whatsMyName",
  ...e
}), U = (e) => (e?.client ?? _).get({
  security: [{ scheme: "bearer", type: "http" }],
  url: "/umbraco/umbracocommunityaiprovenance/api/v1/whatsTheTimeMrWolf",
  ...e
}), N = (e) => (e?.client ?? _).get({
  security: [{ scheme: "bearer", type: "http" }],
  url: "/umbraco/umbracocommunityaiprovenance/api/v1/whoAmI",
  ...e
});
var k = Object.defineProperty, $ = Object.getOwnPropertyDescriptor, b = (e) => {
  throw TypeError(e);
}, s = (e, t, r, a) => {
  for (var o = a > 1 ? void 0 : a ? $(t, r) : t, h = e.length - 1, m; h >= 0; h--)
    (m = e[h]) && (o = (a ? m(t, r, o) : m(o)) || o);
  return a && o && k(t, r, o), o;
}, g = (e, t, r) => t.has(e) || b("Cannot " + r), n = (e, t, r) => (g(e, t, "read from private field"), r ? r.call(e) : t.get(e)), u = (e, t, r) => t.has(e) ? b("Cannot add the same private member more than once") : t instanceof WeakSet ? t.add(e) : t.set(e, r), O = (e, t, r, a) => (g(e, t, "write to private field"), t.set(e, r), r), c, d, f, v;
let i = class extends C(x) {
  constructor() {
    super(), this._yourName = "Press the button!", u(this, c), u(this, d, async (e) => {
      const t = e.target;
      t.state = "waiting";
      const { data: r, error: a } = await p(this, N());
      if (a || !r) {
        t.state = "failed";
        return;
      }
      this._serverUserData = r, t.state = "success", n(this, c)?.peek("warning", {
        data: {
          headline: `You are ${r.name}`,
          message: `Your email is ${r.email}`
        }
      });
    }), u(this, f, async (e) => {
      const t = e.target;
      t.state = "waiting";
      const { data: r, error: a } = await p(this, U());
      if (a) {
        t.state = "failed";
        return;
      }
      r !== void 0 && (this._timeFromMrWolf = new Date(r), t.state = "success");
    }), u(this, v, async (e) => {
      const t = e.target;
      t.state = "waiting";
      const { data: r, error: a } = await p(this, M());
      if (a) {
        t.state = "failed";
        return;
      }
      this._yourName = r, t.state = "success";
    }), this.consumeContext(T, (e) => {
      O(this, c, e);
    }), this.consumeContext(E, (e) => {
      this.observe(
        e?.currentUser,
        (t) => {
          this._contextCurrentUser = t;
        },
        "_contextCurrentUser"
      );
    });
  }
  render() {
    return y`
      <uui-box headline="Who am I?">
        <div slot="header">[Server]</div>
        <h2>
          <uui-icon name="icon-user"></uui-icon>${this._serverUserData?.email ? this._serverUserData.email : "Press the button!"}
        </h2>
        <ul>
          ${this._serverUserData?.groups?.map(
      (e) => y`<li>${e}</li>`
    )}
        </ul>
        <uui-button
          color="default"
          look="primary"
          @click="${n(this, d)}"
        >
          Who am I?
        </uui-button>
        <p>
          This endpoint gets your current user from the server and displays your
          email and list of user groups. It also displays a Notification with
          your details.
        </p>
      </uui-box>

      <uui-box headline="What's my Name?">
        <div slot="header">[Server]</div>
        <h2><uui-icon name="icon-user"></uui-icon> ${this._yourName}</h2>
        <uui-button
          color="default"
          look="primary"
          @click="${n(this, v)}"
        >
          Whats my name?
        </uui-button>
        <p>
          This endpoint has a forced delay to show the button 'waiting' state
          for a few seconds before completing the request.
        </p>
      </uui-box>

      <uui-box headline="What's the Time?">
        <div slot="header">[Server]</div>
        <h2>
          <uui-icon name="icon-alarm-clock"></uui-icon> ${this._timeFromMrWolf ? this._timeFromMrWolf.toLocaleString() : "Press the button!"}
        </h2>
        <uui-button
          color="default"
          look="primary"
          @click="${n(this, f)}"
        >
          Whats the time Mr Wolf?
        </uui-button>
        <p>This endpoint gets the current date and time from the server.</p>
      </uui-box>

      <uui-box headline="Who am I?" class="wide">
        <div slot="header">[Context]</div>
        <p>Current user email: <b>${this._contextCurrentUser?.email}</b></p>
        <p>
          This is the JSON object available by consuming the
          'UMB_CURRENT_USER_CONTEXT' context:
        </p>
        <umb-code-block language="json" copy
          >${JSON.stringify(this._contextCurrentUser, null, 2)}</umb-code-block
        >
      </uui-box>
    `;
  }
};
c = /* @__PURE__ */ new WeakMap();
d = /* @__PURE__ */ new WeakMap();
f = /* @__PURE__ */ new WeakMap();
v = /* @__PURE__ */ new WeakMap();
i.styles = [
  W`
      :host {
        display: grid;
        gap: var(--uui-size-layout-1);
        padding: var(--uui-size-layout-1);
        grid-template-columns: 1fr 1fr 1fr;
      }

      uui-box {
        margin-bottom: var(--uui-size-layout-1);
      }

      h2 {
        margin-top: 0;
      }

      .wide {
        grid-column: span 3;
      }
    `
];
s([
  l()
], i.prototype, "_yourName", 2);
s([
  l()
], i.prototype, "_timeFromMrWolf", 2);
s([
  l()
], i.prototype, "_serverUserData", 2);
s([
  l()
], i.prototype, "_contextCurrentUser", 2);
i = s([
  w("example-dashboard")
], i);
const F = i;
export {
  i as ExampleDashboardElement,
  F as default
};
//# sourceMappingURL=dashboard.element-DCeFYMWX.js.map
