using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tenancy.Api.Migrations;

[DbContext(typeof(TenancyDb))]
[Migration("20261121170000_Initial")]
public sealed class Initial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE TABLE accounts (
          "Id" uuid PRIMARY KEY, "ClerkUserId" varchar(255) NOT NULL UNIQUE,
          "IsPlatformAdministrator" boolean NOT NULL, "Status" text NOT NULL CHECK ("Status" IN ('Active','Deleted','Blocked')),
          "CreatedAt" timestamptz NOT NULL, "UpdatedAt" timestamptz NOT NULL, "LastSeenAt" timestamptz NOT NULL
        );
        CREATE TABLE organisations (
          "Id" uuid PRIMARY KEY, "Name" varchar(256) NOT NULL, "Description" varchar(4000),
          "OwnerAccountId" uuid NOT NULL REFERENCES accounts("Id"), "Status" text NOT NULL CHECK ("Status" IN ('Active','Deleting','Deleted')),
          "AccessRevision" bigint NOT NULL, "Version" bigint NOT NULL,
          "CreatedAt" timestamptz NOT NULL, "UpdatedAt" timestamptz NOT NULL, "DeletedAt" timestamptz
        );
        CREATE UNIQUE INDEX organisations_name_unique ON organisations (lower("Name")) WHERE "Status" <> 'Deleted';
        CREATE TABLE organisation_members (
          "Id" uuid PRIMARY KEY, "OrganisationId" uuid NOT NULL REFERENCES organisations("Id"),
          "AccountId" uuid NOT NULL REFERENCES accounts("Id"), "Status" text NOT NULL CHECK ("Status" IN ('Active','Revoked')),
          "GrantedByAccountId" uuid NOT NULL REFERENCES accounts("Id"), "GrantedAt" timestamptz NOT NULL,
          "RevokedByAccountId" uuid REFERENCES accounts("Id"), "RevokedAt" timestamptz
        );
        CREATE UNIQUE INDEX organisation_members_active_unique ON organisation_members ("OrganisationId", "AccountId") WHERE "Status" = 'Active';
        CREATE TABLE organisation_audit_events (
          "Id" uuid PRIMARY KEY, "OrganisationId" uuid NOT NULL REFERENCES organisations("Id"), "EventType" text NOT NULL,
          "ActorAccountId" uuid NOT NULL REFERENCES accounts("Id"), "OccurredAt" timestamptz NOT NULL
        );
        CREATE TABLE outbox_messages (
          "Id" uuid PRIMARY KEY, "EventType" text NOT NULL, "Subject" text NOT NULL, "Payload" text NOT NULL,
          "OccurredAt" timestamptz NOT NULL, "PublishedAt" timestamptz
        );
        CREATE INDEX outbox_unpublished ON outbox_messages ("OccurredAt") WHERE "PublishedAt" IS NULL;
        """);
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DROP TABLE outbox_messages, organisation_audit_events, organisation_members, organisations, accounts;
        """);
}
