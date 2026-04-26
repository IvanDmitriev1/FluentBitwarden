#pragma once
#include <pch.h>
#include "IpcProtocol.h"

namespace FluentBitwarden::ComServer::Ipc
{
    struct PingRequest
    {
        static constexpr std::uint16_t MessageType = 1;

        winrt::hstring Text;

        [[nodiscard]] JsonObject ToJson() const
        {
            JsonObject json;

            json.Insert(
                L"Text",
                JsonValue::CreateStringValue(Text));

            return json;
        }
    };

    struct PingResponse
    {
        static constexpr std::uint16_t MessageType = 1;

        winrt::hstring Text;
        bool Ok{};

        [[nodiscard]] static PingResponse FromJson(JsonObject const& json)
        {
            return PingResponse
            {
                .Text = json.GetNamedString(L"Text", L""),
                .Ok = json.GetNamedBoolean(L"Ok", false)
            };
        }
    };

}