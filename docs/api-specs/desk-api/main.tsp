import "@typespec/http";
import "@typespec/openapi";

using TypeSpec.Http;
using TypeSpec.OpenAPI;

/**
 * Ticket backend (`otai-desk/line/backend`, LD-090).
 * OAuth2 password bearer for people. Studio uses Authorization: Bearer
 * with the deployment API token (OTAI_DESK_API_TOKEN). Scopes are rights.
 * Tickets store id, ticket_number, title, status, queue, priority, type, and service.
 * Other facts are attributes.
 * Articles store id and meta.
 * FAQs store id, question, answer, and attributes.
 * Links (LD-098) join ticket, FAQ, and configuration item. A link has a type
 * and its own attributes.
 */
@service(#{ title: "OTAI Desk API" })
@server("http://localhost:8000", "Local desk backend")
namespace Otai.Desk;

alias TicketId = string;
alias FAQId = string;
alias ValueKind =
  | "string"
  | "number"
  | "boolean"
  | "date"
  | "datetime"
  | "user"
  | "select";

alias InputKind =
  | "text"
  | "textarea"
  | "number"
  | "date"
  | "timestamp"
  | "checkbox"
  | "select"
  | "user";

alias ViewKind =
  | "text"
  | "prose"
  | "number"
  | "date"
  | "timestamp"
  | "checkbox"
  | "badge"
  | "user";

alias ObjectKind = "ticket" | "faq" | "configuration_item" | "link";

@error
model Unauthorized {
  @statusCode _: 401;
  @body body: {
    detail: string;
  };
}

@error
model Forbidden {
  @statusCode _: 403;
  @body body: {
    detail: string;
  };
}

@error
model NotFound {
  @statusCode _: 404;
  @body body: {
    detail: string;
  };
}

@error
model Invalid {
  @statusCode _: 422;
  @body body: {
    detail: string;
  };
}

model HealthResponse {
  status: "ok";
}

model TokenRequest {
  username: string;
  password: string;
}

model TokenResponse {
  access_token: string;
  token_type: "bearer";
}

model User {
  id: string;
  organization_id: string;
  email: string;
  display_name: string;
  roles: string[];
  is_active: boolean;
}

model UserCreate {
  email: string;
  display_name: string;
  password: string;
  roles: string[];
}

model UserUpdate {
  display_name?: string;
  password?: string;
  roles?: string[];
  is_active?: boolean;
}

model Role {
  id: string;
  name: string;
  rights: string[];
}

model AttributeDefinition {
  id: string;
  name: string;
  label: string;
  value_kind: ValueKind;
  input_kind: InputKind;
  view_kind: ViewKind;
  group: string;
  required: boolean;
  options?: string[];
  object_kind: ObjectKind;
  link_type_id?: string;
}

model AttributeDefinitionCreate {
  name: string;
  label: string;
  value_kind: ValueKind;
  input_kind: InputKind;
  view_kind: ViewKind;
  group: string;
  required: boolean;
  options?: string[];
  object_kind: ObjectKind;
  link_type_id?: string;
}

model Ticket {
  id: TicketId;
  ticket_number: string;
  title: string;
  status: string;
  queue?: string;
  priority?: string;
  type?: string;
  service?: string;
  attributes: Record<string>;
}

model TicketCreate {
  title: string;
  status: string;
  queue?: string;
  priority?: string;
  type?: string;
  service?: string;
  attributes: Record<string>;
}

model TicketUpdate {
  title?: string;
  status?: string;
  queue?: string;
  priority?: string;
  type?: string;
  service?: string;
  attributes?: Record<string>;
}

model ArticleMeta {
  body: string;
  subject: string;
  from: string;
  to: string;
  content_type: string;
  sender_type: string;
  created_at: utcDateTime;
  author_user_id?: string;
  is_internal: boolean;
}

model Article {
  id: string;
  ticket_id: TicketId;
  meta: ArticleMeta;
}

model ArticleCreate {
  meta: ArticleMeta;
}

model FAQ {
  id: FAQId;
  organization_id: string;
  question: string;
  answer: string;
  attributes: Record<string>;
  created_at: utcDateTime;
  updated_at: utcDateTime;
}

model ConfigurationItem {
  id: string;
  organization_id: string;
  name: string;
  kind: string;
  attributes: Record<string>;
  created_at: utcDateTime;
  updated_at: utcDateTime;
}

model LinkType {
  id: string;
  name: string;
  source_label: string;
  target_label: string;
}

model Link {
  id: string;
  type_id: string;
  source_kind: "ticket" | "faq" | "configuration_item";
  source_id: string;
  target_kind: "ticket" | "faq" | "configuration_item";
  target_id: string;
  attributes: Record<string>;
  created_at: utcDateTime;
}

model LinkCreate {
  type_id: string;
  source_kind: "ticket" | "faq" | "configuration_item";
  source_id: string;
  target_kind: "ticket" | "faq" | "configuration_item";
  target_id: string;
  attributes: Record<string>;
}

model FAQCreate {
  question: string;
  answer: string;
  attributes: Record<string>;
}

model FAQUpdate {
  question?: string;
  answer?: string;
  attributes?: Record<string>;
}

model VisibleSlot {
  slot_name: string;
  component_key: string;
  ordinal: int32;
}

model Page {
  key: string;
  route: string;
  label: string;
  in_nav: boolean;
}

model EffectiveLayout {
  page_key: string;
  slots: VisibleSlot[];
}

model CatalogComponent {
  key: string;
  required_right: string;
}

model PageLayoutWrite {
  slots: VisibleSlot[];
}

model PageLayout {
  page_key: string;
  slots: VisibleSlot[];
}

@route("/health")
interface Health {
  @get check(): {
    @statusCode _: 200;
    @body body: HealthResponse;
  };
}

@route("/api/v1/token")
interface Token {
  @post
  login(@body body: TokenRequest):
    | {
        @statusCode _: 200;
        @body body: TokenResponse;
      }
    | Unauthorized;
}

@route("/api/v1/users")
interface Users {
  @get list():
    | {
        @statusCode _: 200;
        @body body: User[];
      }
    | Unauthorized
    | Forbidden;

  @post create(@body body: UserCreate):
    | {
        @statusCode _: 201;
        @body body: User;
      }
    | Unauthorized
    | Forbidden
    | Invalid;

  @route("/me")
  @get
  me():
    | {
        @statusCode _: 200;
        @body body: User;
      }
    | Unauthorized;

  @route("/{user_id}")
  @get
  get(@path user_id: string):
    | {
        @statusCode _: 200;
        @body body: User;
      }
    | Unauthorized
    | Forbidden
    | NotFound;

  @route("/{user_id}")
  @patch
  update(@path user_id: string, @body body: UserUpdate):
    | {
        @statusCode _: 200;
        @body body: User;
      }
    | Unauthorized
    | Forbidden
    | NotFound
    | Invalid;
}

@route("/api/v1/roles")
interface Roles {
  @get list():
    | {
        @statusCode _: 200;
        @body body: Role[];
      }
    | Unauthorized
    | Forbidden;
}

@route("/api/v1/attribute-definitions")
interface AttributeDefinitions {
  @get list():
    | {
        @statusCode _: 200;
        @body body: AttributeDefinition[];
      }
    | Unauthorized
    | Forbidden;

  @post create(@body body: AttributeDefinitionCreate):
    | {
        @statusCode _: 201;
        @body body: AttributeDefinition;
      }
    | Unauthorized
    | Forbidden
    | Invalid;
}

@route("/api/v1/tickets")
interface Tickets {
  @get list():
    | {
        @statusCode _: 200;
        @body body: Ticket[];
      }
    | Unauthorized
    | Forbidden;

  @post create(@body body: TicketCreate):
    | {
        @statusCode _: 201;
        @body body: Ticket;
      }
    | Unauthorized
    | Forbidden
    | Invalid;

  @route("/{ticket_id}")
  @get
  get(@path ticket_id: TicketId):
    | {
        @statusCode _: 200;
        @body body: Ticket;
      }
    | Unauthorized
    | Forbidden
    | NotFound;

  @route("/{ticket_id}")
  @patch
  update(@path ticket_id: TicketId, @body body: TicketUpdate):
    | {
        @statusCode _: 200;
        @body body: Ticket;
      }
    | Unauthorized
    | Forbidden
    | NotFound
    | Invalid;

  @route("/{ticket_id}/articles")
  @get
  listArticles(@path ticket_id: TicketId):
    | {
        @statusCode _: 200;
        @body body: Article[];
      }
    | Unauthorized
    | Forbidden
    | NotFound;

  @route("/{ticket_id}/articles")
  @post
  createArticle(@path ticket_id: TicketId, @body body: ArticleCreate):
    | {
        @statusCode _: 201;
        @body body: Article;
      }
    | Unauthorized
    | Forbidden
    | NotFound
    | Invalid;
}

@route("/api/v1/links")
interface Links {
  @route("/types")
  @get
  listTypes():
    | {
        @statusCode _: 200;
        @body body: LinkType[];
      }
    | Unauthorized
    | Forbidden;

  @route("/by/{object_kind}/{object_id}")
  @get
  listForObject(
    @path object_kind: "ticket" | "faq" | "configuration_item",
    @path object_id: string,
  ):
    | {
        @statusCode _: 200;
        @body body: Link[];
      }
    | Unauthorized
    | Forbidden
    | NotFound;

  @post create(@body body: LinkCreate):
    | {
        @statusCode _: 201;
        @body body: Link;
      }
    | Unauthorized
    | Forbidden
    | Invalid;

  @route("/{link_id}")
  @delete
  delete(@path link_id: string):
    | {
        @statusCode _: 204;
      }
    | Unauthorized
    | Forbidden
    | NotFound;
}

@route("/api/v1/faqs")
interface FAQs {
  @get list():
    | {
        @statusCode _: 200;
        @body body: FAQ[];
      }
    | Unauthorized
    | Forbidden;

  @post create(@body body: FAQCreate):
    | {
        @statusCode _: 201;
        @body body: FAQ;
      }
    | Unauthorized
    | Forbidden
    | Invalid;

  @route("/{faq_id}")
  @get
  get(@path faq_id: FAQId):
    | {
        @statusCode _: 200;
        @body body: FAQ;
      }
    | Unauthorized
    | Forbidden
    | NotFound;

  @route("/{faq_id}")
  @patch
  update(@path faq_id: FAQId, @body body: FAQUpdate):
    | {
        @statusCode _: 200;
        @body body: FAQ;
      }
    | Unauthorized
    | Forbidden
    | NotFound
    | Invalid;
}

@route("/api/v1/pages")
interface Pages {
  @get list():
    | {
        @statusCode _: 200;
        @body body: Page[];
      }
    | Unauthorized;

  @route("/{page_key}")
  @get
  get(@path page_key: string):
    | {
        @statusCode _: 200;
        @body body: Page;
      }
    | Unauthorized
    | Forbidden
    | NotFound;

  @route("/{page_key}/layout")
  @get
  layout(@path page_key: string):
    | {
        @statusCode _: 200;
        @body body: EffectiveLayout;
      }
    | Unauthorized
    | Forbidden
    | NotFound;
}

@route("/api/v1/page-layouts")
interface PageLayouts {
  @route("/catalog")
  @get
  catalog():
    | {
        @statusCode _: 200;
        @body body: CatalogComponent[];
      }
    | Unauthorized
    | Forbidden;

  @route("/{page_key}")
  @put
  put(@path page_key: string, @body body: PageLayoutWrite):
    | {
        @statusCode _: 200;
        @body body: PageLayout;
      }
    | Unauthorized
    | Forbidden
    | NotFound
    | Invalid;
}
