import type { Dispatch, FormEvent, SetStateAction } from "react";
import type { Webhook, WebhookDelivery, WebhookMetrics } from "../../types";

type WebhookEventType = readonly [string, string];
type Props = { webhooks: Webhook[]; deliveries: WebhookDelivery[]; metrics: WebhookMetrics; editing: Webhook | null; isFormOpen: boolean; isSaving: boolean; isRetrying: boolean; name: string; url: string; secret: string; events: string[]; eventTypes: readonly WebhookEventType[]; onOpenForm: (webhook?: Webhook) => void; onSubmit: (event: FormEvent<HTMLFormElement>) => void; onCancel: () => void; onNameChange: (value: string) => void; onUrlChange: (value: string) => void; onSecretChange: (value: string) => void; onEventsChange: Dispatch<SetStateAction<string[]>>; onToggle: (webhook: Webhook) => void; onDelete: (webhook: Webhook) => void; onRefresh: () => void; onRetryFailed: () => void; };

export function WebhooksTab(props: Props) {
  const { webhooks, deliveries: webhookDeliveries, metrics: webhookMetrics, editing: editingWebhook, isFormOpen: isWebhookFormOpen, isSaving, isRetrying: isRetryingWebhooks, name: webhookName, url: webhookUrl, secret: webhookSecret, events: webhookEvents, eventTypes: webhookEventTypes, onOpenForm: openWebhookForm, onSubmit: saveWebhook, onCancel, onNameChange: setWebhookName, onUrlChange: setWebhookUrl, onSecretChange: setWebhookSecret, onEventsChange: setWebhookEvents, onToggle: toggleWebhook, onDelete: deleteWebhook, onRefresh: loadWebhooks, onRetryFailed: retryFailedWebhooks } = props;
  return (
                  <section className="project-panel tab-panel">
                    <div className="panel-heading">
                      <div>
                        <p className="eyebrow">Webhooks</p>
                        <h2>Project event delivery</h2>
                      </div>
                      <button
                        className="new-button"
                        type="button"
                        onClick={() => openWebhookForm()}
                      >
                        New webhook
                      </button>
                    </div>
                    <p className="webhook-security-note">
                      Deliveries use HTTPS, except for the internal admin service,
                      and include an HMAC-SHA256 signature
                      in <code>X-Agent-Platform-Signature</code>. Use the event
                      ID to handle retries idempotently.
                    </p>
                    {isWebhookFormOpen && (
                      <form className="agent-form" onSubmit={saveWebhook}>
                        <label htmlFor="webhook-name">Name</label>
                        <input
                          id="webhook-name"
                          value={webhookName}
                          maxLength={200}
                          onChange={(event) =>
                            setWebhookName(event.target.value)
                          }
                          required
                        />
                        <div className="webhook-url-label">
                          <label htmlFor="webhook-url">
                            HTTPS endpoint <small>(HTTP is allowed for the internal admin service)</small>
                          </label>
                          <button
                            className="text-button"
                            type="button"
                            onClick={() =>
                              setWebhookUrl(
                                "http://localhost:8080/webhooks/receive",
                              )
                            }
                          >
                            Use built-in receiver
                          </button>
                        </div>
                        <input
                          id="webhook-url"
                          type="url"
                          value={webhookUrl}
                          maxLength={2000}
                          onChange={(event) =>
                            setWebhookUrl(event.target.value)
                          }
                          placeholder="https://receiver.example/events"
                          required
                        />
                        <label htmlFor="webhook-secret">
                          Signing secret{" "}
                          {editingWebhook && (
                            <small>(leave blank to keep current secret)</small>
                          )}
                        </label>
                        <input
                          id="webhook-secret"
                          type="password"
                          minLength={32}
                          maxLength={1000}
                          autoComplete="new-password"
                          value={webhookSecret}
                          onChange={(event) =>
                            setWebhookSecret(event.target.value)
                          }
                          required={!editingWebhook}
                        />
                        <fieldset className="webhook-event-options">
                          <legend>Events to send</legend>
                          {webhookEventTypes.map(([eventType, label]) => (
                            <label key={eventType}>
                              <input
                                type="checkbox"
                                checked={webhookEvents.includes(eventType)}
                                onChange={(event) =>
                                  setWebhookEvents((current) =>
                                    event.target.checked
                                      ? [...current, eventType]
                                      : current.filter(
                                          (item) => item !== eventType,
                                        ),
                                  )
                                }
                              />
                              {label}
                            </label>
                          ))}
                        </fieldset>
                        <div>
                          <button
                            className="new-button"
                            disabled={isSaving || webhookEvents.length === 0}
                          >
                            {isSaving
                              ? "Saving…"
                              : editingWebhook
                                ? "Save changes"
                                : "Create webhook"}
                          </button>
                          <button
                            className="text-button"
                            type="button"
                            onClick={onCancel}
                          >
                            Cancel
                          </button>
                        </div>
                      </form>
                    )}
                    <div
                      className="webhook-metrics"
                      aria-label="Webhook delivery metrics"
                    >
                      <div>
                        <span>Sent</span>
                        <strong>{webhookMetrics.sent}</strong>
                      </div>
                      <div>
                        <span>Pending</span>
                        <strong>{webhookMetrics.pending}</strong>
                      </div>
                      <div>
                        <span>Failed</span>
                        <strong>{webhookMetrics.failed}</strong>
                      </div>
                      <div>
                        <span>Attempts</span>
                        <strong>{webhookMetrics.attempts}</strong>
                      </div>
                    </div>
                    {webhooks.length === 0 ? (
                      <div className="empty-state">
                        <strong>No webhooks configured</strong>
                        <span>
                          Add a secure HTTPS receiver and select the project
                          events it should receive.
                        </span>
                      </div>
                    ) : (
                      <ul className="project-list">
                        {webhooks.map((webhook) => (
                          <li key={webhook.id}>
                            <span className="project-badge">↗</span>
                            <div className="webhook-item-copy">
                              <strong>
                                {webhook.name}
                                {!webhook.isEnabled && (
                                  <span className="webhook-disabled">
                                    Disabled
                                  </span>
                                )}
                              </strong>
                              <small>{webhook.endpointUrl}</small>
                              <small>{webhook.events.join(" · ")}</small>
                            </div>
                            <div className="project-actions">
                              <button
                                onClick={() => void toggleWebhook(webhook)}
                              >
                                {webhook.isEnabled ? "Disable" : "Enable"}
                              </button>
                              <button onClick={() => openWebhookForm(webhook)}>
                                Edit
                              </button>
                              <button
                                onClick={() => void deleteWebhook(webhook)}
                              >
                                Delete
                              </button>
                            </div>
                          </li>
                        ))}
                      </ul>
                    )}
                    <section
                      className="webhook-deliveries"
                      aria-labelledby="webhook-deliveries-title"
                    >
                      <div className="deliveries-heading">
                        <div>
                          <p className="eyebrow">Delivery queue</p>
                          <h3 id="webhook-deliveries-title">
                            Pending and failed messages
                          </h3>
                        </div>
                        <div className="delivery-actions">
                          <button
                            className="text-button"
                            type="button"
                            onClick={() => void loadWebhooks()}
                          >
                            Refresh
                          </button>
                          <button
                            className="text-button"
                            type="button"
                            disabled={
                              webhookMetrics.failed === 0 || isRetryingWebhooks
                            }
                            onClick={() => void retryFailedWebhooks()}
                          >
                            {isRetryingWebhooks
                              ? "Resending…"
                              : `Resend all failed (${webhookMetrics.failed})`}
                          </button>
                        </div>
                      </div>
                      {webhookDeliveries.length === 0 ? (
                        <div className="empty-state compact-empty">
                          <strong>No pending or failed messages</strong>
                          <span>
                            Delivery activity will appear here when subscribed
                            events are emitted.
                          </span>
                        </div>
                      ) : (
                        <div className="delivery-table-wrap">
                          <table className="delivery-table">
                            <thead>
                              <tr>
                                <th scope="col">Webhook</th>
                                <th scope="col">Event</th>
                                <th scope="col">Status</th>
                                <th scope="col">Attempts</th>
                                <th scope="col">Last result</th>
                                <th scope="col">Queued</th>
                              </tr>
                            </thead>
                            <tbody>
                              {webhookDeliveries.map((delivery) => (
                                <tr key={delivery.id}>
                                  <th scope="row">{delivery.webhookName}</th>
                                  <td>{delivery.eventType}</td>
                                  <td>
                                    <span
                                      className={`delivery-status ${delivery.status.toLowerCase()}`}
                                    >
                                      {delivery.status}
                                    </span>
                                  </td>
                                  <td>{delivery.attemptCount}</td>
                                  <td>
                                    {delivery.lastStatusCode
                                      ? `HTTP ${delivery.lastStatusCode}`
                                      : delivery.lastError || "Waiting to send"}
                                    {delivery.lastError &&
                                      delivery.lastStatusCode && (
                                        <small className="delivery-error">
                                          {delivery.lastError}
                                        </small>
                                      )}
                                  </td>
                                  <td>
                                    {new Intl.DateTimeFormat(undefined, {
                                      dateStyle: "medium",
                                      timeStyle: "short",
                                    }).format(new Date(delivery.createdAt))}
                                  </td>
                                </tr>
                              ))}
                            </tbody>
                          </table>
                        </div>
                      )}
                    </section>
                  </section>
  );
}
