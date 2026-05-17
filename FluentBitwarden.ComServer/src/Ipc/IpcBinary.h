#pragma once
#include <concepts>
#include <span>
#include <cstring>
#include <limits>
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
		void WriteUInt16(std::uint16_t value)
		{
			WriteInteger(value);
		}

		void WriteUInt32(std::uint32_t value)
		{
			WriteInteger(value);
		}

		void WriteBytes(std::span<const std::uint8_t> value)
		{
			WriteLength(value.size());
			AppendBytes(std::as_bytes(value));
		}

		void WriteString(std::string_view value)
		{
			WriteLength(value.size());
			AppendBytes(std::as_bytes(std::span{ value.data(), value.size() }));
		}

		[[nodiscard]] std::vector<std::byte> Take() &&
		{
			return std::move(m_buffer);
		}

	private:
		template <std::unsigned_integral T>
		void WriteInteger(T value)
		{
			const auto offset = m_buffer.size();
			m_buffer.resize(offset + sizeof(T));
			WriteLe<T>(std::span<std::byte>{ m_buffer }.subspan(offset, sizeof(T)), value);
		}

		void WriteLength(std::size_t length)
		{
			if (length > std::numeric_limits<std::uint32_t>::max())
			{
				throw std::runtime_error("IPC field length exceeds uint32 range.");
			}

			WriteUInt32(static_cast<std::uint32_t>(length));
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

		[[nodiscard]] std::uint16_t ReadUInt16()
		{
			return ReadInteger<std::uint16_t>();
		}

		[[nodiscard]] std::uint32_t ReadUInt32()
		{
			return ReadInteger<std::uint32_t>();
		}

		[[nodiscard]] std::vector<std::uint8_t> ReadBytes()
		{
			const auto bytes = ReadRaw(ReadUInt32());
			std::vector<std::uint8_t> value(bytes.size());
			if (!bytes.empty())
			{
				std::memcpy(value.data(), bytes.data(), bytes.size());
			}

			return value;
		}

		[[nodiscard]] std::string ReadString()
		{
			const auto bytes = ReadRaw(ReadUInt32());
			if (bytes.empty())
			{
				return {};
			}

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
		template <std::unsigned_integral T>
		[[nodiscard]] T ReadInteger()
		{
			return ReadLe<T>(ReadRaw(sizeof(T)));
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
