#pragma once
#include <concepts>
#include <span>
#include <cstring>
#include <stdexcept>
#include <string>
#include <string_view>
#include <vector>

namespace FluentBitwarden::ComServer::Ipc::Binary
{
	static_assert(std::endian::native == std::endian::little,
				  "This IPC code assumes a little-endian Windows target.");

	template <std::unsigned_integral T>
	[[nodiscard]] T ReadLe(std::span<const std::byte> bytes)
	{
		if (bytes.size() != sizeof(T))
		{
			throw std::invalid_argument("Invalid little-endian integer size.");
		}

		T value{};
		std::memcpy(&value, bytes.data(), sizeof(T));
		return value;
	}

	template <std::unsigned_integral T>
	void WriteLe(std::span<std::byte> bytes, T value)
	{
		if (bytes.size() != sizeof(T))
		{
			throw std::invalid_argument("Invalid little-endian integer size.");
		}

		std::memcpy(bytes.data(), &value, sizeof(T));
	}

	class PayloadWriter final
	{
	public:
		void WriteObjectHeader(std::uint8_t memberCount)
		{
			m_buffer.push_back(static_cast<std::byte>(memberCount));
		}

		void WriteBytes(std::span<const std::uint8_t> value)
		{
			WriteCollectionLength(value.size());
			AppendBytes(std::as_bytes(value));
		}

		void WriteString(std::string_view value)
		{
			if (value.empty())
			{
				WriteInt32(0);
				return;
			}

			WriteInt32(~static_cast<std::int32_t>(value.size()));
			WriteInt32(-1);
			AppendBytes(std::as_bytes(std::span{ value.data(), value.size() }));
		}

		[[nodiscard]] std::vector<std::byte> Take() &&
		{
			return std::move(m_buffer);
		}

	private:
		void WriteInt32(std::int32_t value)
		{
			const auto offset = m_buffer.size();
			m_buffer.resize(offset + sizeof(value));
			std::memcpy(m_buffer.data() + offset, &value, sizeof(value));
		}

		void WriteCollectionLength(std::size_t length)
		{
			WriteInt32(static_cast<std::int32_t>(length));
		}

		void AppendBytes(std::span<const std::byte> bytes)
		{
			const auto offset = m_buffer.size();
			m_buffer.resize(offset + bytes.size());
			if (bytes.empty())
			{
				return;
			}

			std::memcpy(m_buffer.data() + offset, bytes.data(), bytes.size());
		}

	private:
		std::vector<std::byte> m_buffer;
	};

	class PayloadReader final
	{
	public:
		explicit PayloadReader(std::span<const std::byte> payload)
			: m_payload(payload)
		{
		}

		void ReadObjectHeader(std::uint8_t expectedMemberCount)
		{
			const auto actual = ReadRaw(sizeof(std::uint8_t))[0];
			if (actual != static_cast<std::byte>(expectedMemberCount))
			{
				throw std::runtime_error("Unexpected MemoryPack object member count.");
			}
		}

		[[nodiscard]] std::vector<std::uint8_t> ReadBytes()
		{
			const auto bytes = ReadRaw(ReadCollectionLength());
			std::vector<std::uint8_t> value(bytes.size());
			if (!bytes.empty())
			{
				std::memcpy(value.data(), bytes.data(), bytes.size());
			}

			return value;
		}

		[[nodiscard]] std::string ReadString()
		{
			const auto length = ReadInt32();
			if (length == 0)
			{
				return {};
			}

			if (length > 0)
			{
				throw std::runtime_error("Expected MemoryPack UTF-8 string.");
			}

			const auto utf8Length = ~length;
			(void)ReadInt32();

			const auto bytes = ReadRaw(static_cast<std::size_t>(utf8Length));
			return std::string{
				reinterpret_cast<const char*>(bytes.data()),
				bytes.size()
			};
		}

		void EnsureConsumed() const
		{
			if (m_offset != m_payload.size())
			{
				throw std::runtime_error("Unexpected trailing IPC payload bytes.");
			}
		}

	private:
		[[nodiscard]] std::int32_t ReadInt32()
		{
			std::int32_t value{};
			const auto bytes = ReadRaw(sizeof(value));
			std::memcpy(&value, bytes.data(), sizeof(value));
			return value;
		}

		[[nodiscard]] std::size_t ReadCollectionLength()
		{
			const auto length = ReadInt32();
			if (length < 0)
			{
				throw std::runtime_error("Unexpected null MemoryPack collection.");
			}

			return static_cast<std::size_t>(length);
		}

		[[nodiscard]] std::span<const std::byte> ReadRaw(std::size_t length)
		{
			if (length > m_payload.size() - m_offset)
			{
				throw std::runtime_error("IPC payload ended before the current field was complete.");
			}

			const auto bytes = m_payload.subspan(m_offset, length);
			m_offset += length;
			return bytes;
		}

	private:
		std::span<const std::byte> m_payload;
		std::size_t m_offset{};
	};
}
