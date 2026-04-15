#pragma once
#include <array>
#include <vector>
#include <cstdint>
#include <string_view>
#include <span>

class CborWriter final
{
public:
	using Byte = unsigned char;

	CborWriter() = default;

	CborWriter(const CborWriter&) = delete;
	CborWriter& operator=(const CborWriter&) = delete;

	CborWriter(CborWriter&&) = default;
	CborWriter& operator=(CborWriter&&) = default;


public:
	void WriteMap(std::size_t itemCount);
	void WriteArrayHeader(std::size_t itemCount);

	void WriteKey(std::uint64_t value);
	void WriteUnsigned(std::uint64_t value);
	void WriteInteger(std::int64_t value);
	void WriteBool(bool value);
	void WriteText(std::string_view text);
	void WriteBytes(std::span<const std::uint8_t> bytes);

	void WriteTextArray(std::span<const std::string_view> values);

	[[nodiscard]] std::vector<std::uint8_t> Finish();

private:
	[[nodiscard]] static std::vector<char> ToTextBuffer(std::string_view text);
	[[nodiscard]] static std::vector<Byte> ToByteBuffer(std::span<const std::uint8_t> bytes);

private:
	std::vector<Byte> m_buffer;
	std::size_t m_size = 0;
};
